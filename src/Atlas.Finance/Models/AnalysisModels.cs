using System;
using System.Collections.Generic;
using Atlas.Finance.Models.ICT;
using Atlas.Finance.Models.Technical;
using Atlas.Finance.Models.Fundamental;
using Atlas.Finance.Models.Analysis;

namespace Atlas.Finance.Models.Analysis;

public class HistoricalPrice
{
    public decimal Price { get; set; }
    public DateTime Timestamp { get; set; }
}

