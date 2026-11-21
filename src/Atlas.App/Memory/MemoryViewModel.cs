using System.Collections.ObjectModel;
using Atlas.App.Mvvm;
using Atlas.Core.Interfaces;
using Atlas.Core.Models;

namespace Atlas.App.Memory;

public sealed class MemoryItemViewModel : ObservableObject
{
    private int _importance;

    public MemoryItemViewModel(MemoryEntry entry)
    {
        Id = entry.Id;
        Content = entry.Content;
        Kind = entry.Kind.ToString();
        Updated = entry.UpdatedAt.ToLocalTime();
        _importance = entry.Importance;
    }

    public long Id { get; }
    public string Content { get; }
    public string Kind { get; }
    public DateTime Updated { get; }
    public string UpdatedText => Updated.ToString("d MMM yyyy");

    public int Importance
    {
        get => _importance;
        set
        {
            if (Set(ref _importance, value))
                OnPropertyChanged(nameof(IsPinned));
        }
    }

    public bool IsPinned => Importance >= 5;
}

/// <summary>Browse, search, add, pin and delete what Atlas remembers.</summary>
public sealed class MemoryViewModel : ObservableObject
{
    private readonly IMemoryService _memory;
    private string _searchText = "";
    private string _newMemoryText = "";
    private string _status = "";

    public MemoryViewModel(IMemoryService memory)
    {
        _memory = memory;

        AddCommand = new RelayCommand(Add, () => !string.IsNullOrWhiteSpace(NewMemoryText));
        DeleteCommand = new RelayCommand(p => { if (p is MemoryItemViewModel item) Delete(item); });
        TogglePinCommand = new RelayCommand(p => { if (p is MemoryItemViewModel item) TogglePin(item); });
        ClearSearchCommand = new RelayCommand(() => SearchText = "");

        Refresh();
    }

    public ObservableCollection<MemoryItemViewModel> Items { get; } = new();

    public RelayCommand AddCommand { get; }
    public RelayCommand DeleteCommand { get; }
    public RelayCommand TogglePinCommand { get; }
    public RelayCommand ClearSearchCommand { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (Set(ref _searchText, value))
                Refresh();
        }
    }

    public string NewMemoryText
    {
        get => _newMemoryText;
        set
        {
            if (Set(ref _newMemoryText, value))
                AddCommand.RaiseCanExecuteChanged();
        }
    }

    public string Status
    {
        get => _status;
        private set => Set(ref _status, value);
    }

    public bool IsEmpty => Items.Count == 0;

    public bool IsSearching => !string.IsNullOrWhiteSpace(SearchText);

    public string EmptyText => IsSearching
        ? "Nothing matches that search."
        : "Atlas hasn't been told to remember anything yet.\nTry saying \"remember that my sister is called Ola\".";

    public string CountText
    {
        get
        {
            int total = _memory.Count();
            return total == 1 ? "1 memory" : $"{total} memories";
        }
    }

    public void Refresh()
    {
        IReadOnlyList<MemoryEntry> entries = IsSearching
            ? _memory.Search(SearchText, 50)
            : _memory.List(limit: 200);

        Items.Clear();

        // Pinned first, then most recently updated (search keeps its relevance order).
        IEnumerable<MemoryEntry> ordered = IsSearching
            ? entries
            : entries.OrderByDescending(e => e.Importance >= 5).ThenByDescending(e => e.UpdatedAt);

        foreach (MemoryEntry entry in ordered)
            Items.Add(new MemoryItemViewModel(entry));

        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(IsSearching));
        OnPropertyChanged(nameof(EmptyText));
        OnPropertyChanged(nameof(CountText));
    }

    private void Add()
    {
        string text = NewMemoryText.Trim();
        if (text.Length == 0)
            return;

        int before = _memory.Count();
        _memory.Remember(text, source: "ui");

        NewMemoryText = "";
        Status = _memory.Count() > before ? "Saved." : "Atlas already knew that.";
        Refresh();
    }

    private void Delete(MemoryItemViewModel item)
    {
        if (_memory.Forget(item.Id))
            Status = "Forgotten.";

        Refresh();
    }

    private void TogglePin(MemoryItemViewModel item)
    {
        int next = item.IsPinned ? 3 : 5;
        if (_memory.SetImportance(item.Id, next))
        {
            item.Importance = next;
            Status = next == 5 ? "Pinned: Atlas will always keep this in mind." : "Unpinned.";
        }

        Refresh();
    }
}
