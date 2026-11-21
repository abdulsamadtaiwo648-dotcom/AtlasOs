(() => {
  "use strict";

  // ---------- small helpers ----------
  const $ = (id) => document.getElementById(id);

  function el(tag, className, text) {
    const node = document.createElement(tag);
    if (className) node.className = className;
    if (text !== undefined) node.textContent = text;   // textContent only: never inject HTML
    return node;
  }

  const store = {
    get(key) { try { return localStorage.getItem(key); } catch { return null; } },
    set(key, value) { try { localStorage.setItem(key, value); } catch { /* private mode */ } },
    remove(key) { try { localStorage.removeItem(key); } catch { /* ignore */ } }
  };

  let token = store.get("atlas.token") || "";
  let busy = false;
  let abortController = null;
  let speakReplies = store.get("atlas.speak") === "1";

  // A link like http://pc:5050/#t=PASSWORD signs in once; the password is then removed from the address bar.
  function readTokenFromLink() {
    const match = /(?:^|[#&])t=([^&]+)/.exec(location.hash);
    if (!match) return false;

    token = decodeURIComponent(match[1]);
    store.set("atlas.token", token);
    history.replaceState(null, "", location.pathname + location.search);
    return true;
  }

  readTokenFromLink();

  // ---------- talking to the PC ----------
  class AuthError extends Error {}

  async function api(path, options = {}) {
    const headers = Object.assign({ Authorization: "Bearer " + token }, options.headers || {});
    if (options.json !== undefined) {
      headers["Content-Type"] = "application/json";
      options.body = JSON.stringify(options.json);
    }

    let response;
    try {
      response = await fetch(path, Object.assign({}, options, { headers }));
    } catch (err) {
      if (err.name === "AbortError") throw err;
      throw new Error("Can't reach your PC. Is Atlas Server running and are you on the same Wi-Fi?");
    }

    if (response.status === 401) { throw new AuthError("Wrong password."); }
    if (response.status === 429) { throw new Error("Too many wrong passwords. Wait a few minutes."); }

    return response;
  }

  async function apiJson(path, options) {
    const response = await api(path, options);
    if (!response.ok) {
      let message = "Something went wrong (" + response.status + ").";
      try { message = (await response.json()).error || message; } catch { /* not JSON */ }
      throw new Error(message);
    }
    return response.status === 204 ? null : response.json();
  }

  // ---------- screens ----------
  function showLogin(message) {
    $("app").classList.add("hidden");
    $("login").classList.remove("hidden");
    $("loginError").textContent = message || "";
    $("tokenInput").focus();
  }

  function showApp() {
    $("login").classList.add("hidden");
    $("app").classList.remove("hidden");
  }

  function toast(message) {
    const t = $("toast");
    t.textContent = message;
    t.classList.remove("hidden");
    clearTimeout(toast.timer);
    toast.timer = setTimeout(() => t.classList.add("hidden"), 3500);
  }

  function handleError(err) {
    if (err instanceof AuthError) { store.remove("atlas.token"); token = ""; showLogin("Wrong password. Check the one on your PC."); return; }
    if (err.name === "AbortError") return;
    toast(err.message || "Something went wrong.");
  }

  // ---------- chat ----------
  const messagesEl = $("messages");
  const scroller = $("chatView");

  function nearBottom() {
    return scroller.scrollHeight - scroller.scrollTop - scroller.clientHeight < 80;
  }

  function scrollToBottom() { scroller.scrollTop = scroller.scrollHeight; }

  function addMessage(role, text) {
    const wrap = el("div", "msg " + role);
    let body;

    if (role === "assistant") {
      wrap.appendChild(el("div", "who", "Atlas"));
      body = el("div", "body", text);
      wrap.appendChild(body);
    } else {
      wrap.textContent = text;
      body = wrap;
    }

    messagesEl.appendChild(wrap);
    updateEmpty();
    return body;
  }

  function updateEmpty() {
    $("empty").classList.toggle("hidden", messagesEl.children.length > 0);
  }

  function renderConversation(conversation) {
    messagesEl.replaceChildren();
    for (const m of conversation.messages) addMessage(m.role, m.text);
    $("title").textContent = conversation.title || "Atlas";
    updateEmpty();
    scrollToBottom();
  }

  async function loadChat() {
    renderConversation(await apiJson("/api/chat"));
  }

  function setBusy(value) {
    busy = value;
    $("sendBtn").classList.toggle("hidden", value);
    $("stopBtn").classList.toggle("hidden", !value);
    $("newBtn").disabled = value;
  }

  async function send() {
    const input = $("input");
    const text = input.value.trim();
    if (!text || busy) return;

    stopSpeaking();
    input.value = "";
    autosize();
    addMessage("user", text);
    const body = addMessage("assistant", "");
    body.classList.add("streaming");
    scrollToBottom();
    setBusy(true);
    abortController = new AbortController();

    let reply = "";
    try {
      const response = await api("/api/chat", { method: "POST", json: { text }, signal: abortController.signal });

      if (!response.ok) {
        let message = "Something went wrong (" + response.status + ").";
        try { message = (await response.json()).error || message; } catch { /* ignore */ }
        throw new Error(message);
      }

      const reader = response.body.getReader();
      const decoder = new TextDecoder();

      for (;;) {
        const { done, value } = await reader.read();
        if (done) break;

        const follow = nearBottom();
        reply += decoder.decode(value, { stream: true });
        body.textContent = reply;
        if (follow) scrollToBottom();
      }
    } catch (err) {
      if (!reply) body.parentElement.remove();
      handleError(err);
    } finally {
      body.classList.remove("streaming");
      setBusy(false);
      abortController = null;
      updateEmpty();
    }

    if (reply) {
      if (speakReplies) speak(reply);
      refreshTitle();
    }
  }

  async function refreshTitle() {
    try { renderTitleOnly(await apiJson("/api/chat")); } catch { /* not important */ }
  }

  function renderTitleOnly(conversation) { $("title").textContent = conversation.title || "Atlas"; }

  async function newChat() {
    if (busy) return;
    try {
      await apiJson("/api/chat/new", { method: "POST" });
      await loadChat();
      closeDrawer();
      setTab("chat");
    } catch (err) { handleError(err); }
  }

  // Auto-growing input; Enter sends on desktop keyboards, Shift+Enter adds a line.
  function autosize() {
    const input = $("input");
    input.style.height = "auto";
    input.style.height = Math.min(input.scrollHeight, 140) + "px";
  }

  // ---------- speaking and dictation ----------
  function speak(text) {
    if (!("speechSynthesis" in window)) return;
    stopSpeaking();
    const utterance = new SpeechSynthesisUtterance(text);
    window.speechSynthesis.speak(utterance);
  }

  function stopSpeaking() {
    if ("speechSynthesis" in window) window.speechSynthesis.cancel();
  }

  function setSpeakReplies(value) {
    speakReplies = value;
    store.set("atlas.speak", value ? "1" : "0");
    $("speakBtn").classList.toggle("on", value);
    $("speakBtn").setAttribute("aria-pressed", String(value));
    if (!value) stopSpeaking();
  }

  function setUpMic() {
    const Recognition = window.SpeechRecognition || window.webkitSpeechRecognition;

    // Browsers only allow the microphone on secure (HTTPS) pages. Plain http://192.168... is not secure,
    // so there we point to the keyboard's own dictation button instead.
    if (!Recognition || !window.isSecureContext) {
      $("micHint").classList.remove("hidden");
      return;
    }

    const mic = $("micBtn");
    mic.classList.remove("hidden");

    let recognizer = null;

    mic.addEventListener("click", () => {
      if (recognizer) { recognizer.stop(); return; }

      recognizer = new Recognition();
      recognizer.interimResults = true;
      recognizer.lang = navigator.language || "en-US";
      const before = $("input").value;

      recognizer.onresult = (event) => {
        let spoken = "";
        for (const result of event.results) spoken += result[0].transcript;
        $("input").value = (before ? before + " " : "") + spoken;
        autosize();
      };
      recognizer.onerror = () => toast("Couldn't use the microphone here.");
      recognizer.onend = () => { mic.classList.remove("recording"); recognizer = null; };

      mic.classList.add("recording");
      recognizer.start();
    });
  }

  // ---------- recent chats drawer ----------
  function openDrawer() {
    $("drawer").classList.add("open");
    $("scrim").classList.remove("hidden");
    loadChatList();
  }

  function closeDrawer() {
    $("drawer").classList.remove("open");
    $("scrim").classList.add("hidden");
  }

  async function loadChatList() {
    try {
      const chats = await apiJson("/api/chats");
      const list = $("chatList");
      list.replaceChildren();

      if (chats.length === 0) {
        list.appendChild(el("p", "muted small-text", "No saved chats yet."));
        return;
      }

      for (const chat of chats) {
        const row = el("div", "chat-item" + (chat.current ? " current" : ""));

        const open = el("button", "open", chat.title);
        open.addEventListener("click", async () => {
          if (busy) return;
          try {
            renderConversation(await apiJson("/api/chats/" + chat.id + "/open", { method: "POST" }));
            closeDrawer();
            setTab("chat");
          } catch (err) { handleError(err); }
        });

        const del = el("button", "del icon", "✕");
        del.setAttribute("aria-label", "Delete chat");
        del.addEventListener("click", async () => {
          if (busy || !confirm("Delete this chat?")) return;
          try {
            await apiJson("/api/chats/" + chat.id, { method: "DELETE" });
            await loadChat();
            await loadChatList();
          } catch (err) { handleError(err); }
        });

        row.append(open, del);
        list.appendChild(row);
      }
    } catch (err) { handleError(err); }
  }

  // ---------- memory ----------
  let searchTimer = null;

  async function loadMemory() {
    const q = $("memorySearch").value.trim();
    try {
      const items = await apiJson("/api/memory" + (q ? "?q=" + encodeURIComponent(q) : ""));
      const list = $("memoryList");
      list.replaceChildren();

      const empty = $("memoryEmpty");
      empty.classList.toggle("hidden", items.length > 0);
      empty.textContent = q
        ? "Nothing matches that search."
        : "Atlas hasn't been told to remember anything yet.";

      for (const item of items) {
        const card = el("div", "mem");
        const text = el("div", "text");
        text.appendChild(el("div", "", item.content));

        const meta = el("div", "meta", item.kind + " · " + new Date(item.updatedAt).toLocaleDateString());
        if (item.pinned) meta.appendChild(el("span", "pin", " · Pinned"));
        text.appendChild(meta);

        const pin = el("button", "icon" + (item.pinned ? " on" : ""), "📌");
        pin.setAttribute("aria-label", item.pinned ? "Unpin" : "Pin: always keep in mind");
        pin.addEventListener("click", async () => {
          try {
            await apiJson("/api/memory/" + item.id + "/pin", { method: "POST", json: { pinned: !item.pinned } });
            loadMemory();
          } catch (err) { handleError(err); }
        });

        const del = el("button", "icon", "✕");
        del.setAttribute("aria-label", "Forget this");
        del.addEventListener("click", async () => {
          if (!confirm("Make Atlas forget this?")) return;
          try {
            await apiJson("/api/memory/" + item.id, { method: "DELETE" });
            $("memoryStatus").textContent = "Forgotten.";
            loadMemory();
          } catch (err) { handleError(err); }
        });

        card.append(text, pin, del);
        list.appendChild(card);
      }
    } catch (err) { handleError(err); }
  }

  async function addMemory() {
    const input = $("memoryNew");
    const text = input.value.trim();
    if (!text) return;

    try {
      const result = await apiJson("/api/memory", { method: "POST", json: { text } });
      input.value = "";
      $("memoryStatus").textContent = result.alreadyKnown ? "Atlas already knew that." : "Saved.";
      loadMemory();
    } catch (err) { handleError(err); }
  }

  // ---------- tabs ----------
  function setTab(name) {
    const chat = name === "chat";
    $("chatView").classList.toggle("hidden", !chat);
    $("memoryView").classList.toggle("hidden", chat);
    $("composer").classList.toggle("hidden", !chat);
    $("tabChat").classList.toggle("active", chat);
    $("tabMemory").classList.toggle("active", !chat);
    if (!chat) loadMemory(); else scrollToBottom();
  }

  // ---------- start-up ----------
  async function connect() {
    try {
      const status = await apiJson("/api/status");
      showApp();
      $("title").title = "Model: " + status.model;
      await loadChat();
    } catch (err) {
      if (err instanceof AuthError) showLogin(token ? "Wrong password. Check the one on your PC." : "");
      else { showLogin(err.message); }
    }
  }

  $("connectBtn").addEventListener("click", () => {
    token = $("tokenInput").value.trim();
    if (!token) return;
    store.set("atlas.token", token);
    $("tokenInput").value = "";
    connect();
  });
  $("tokenInput").addEventListener("keydown", (e) => { if (e.key === "Enter") $("connectBtn").click(); });

  $("sendBtn").addEventListener("click", send);
  $("stopBtn").addEventListener("click", () => abortController && abortController.abort());
  $("input").addEventListener("input", autosize);
  $("input").addEventListener("keydown", (e) => {
    // On touch keyboards Enter makes a new line; on a physical keyboard Enter sends.
    if (e.key === "Enter" && !e.shiftKey && !/Android|iPhone|iPad/i.test(navigator.userAgent)) {
      e.preventDefault();
      send();
    }
  });

  $("newBtn").addEventListener("click", newChat);
  $("menuBtn").addEventListener("click", openDrawer);
  $("drawerClose").addEventListener("click", closeDrawer);
  $("scrim").addEventListener("click", closeDrawer);
  $("speakBtn").addEventListener("click", () => setSpeakReplies(!speakReplies));
  $("tabChat").addEventListener("click", () => setTab("chat"));
  $("tabMemory").addEventListener("click", () => setTab("memory"));
  $("memoryAdd").addEventListener("click", addMemory);
  $("memoryNew").addEventListener("keydown", (e) => { if (e.key === "Enter") addMemory(); });
  $("memorySearch").addEventListener("input", () => { clearTimeout(searchTimer); searchTimer = setTimeout(loadMemory, 250); });

  // Opening a new sign-in link while the page is already open changes only the address fragment.
  window.addEventListener("hashchange", () => { if (readTokenFromLink()) connect(); });

  setSpeakReplies(speakReplies);
  setUpMic();

  if (token) connect(); else showLogin("");
})();
