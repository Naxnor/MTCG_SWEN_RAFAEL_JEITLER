using Newtonsoft.Json;

namespace MTCG.Models;

public class TradingDeal
{
    [JsonProperty("Id")]
    public Guid Id { get; set; }

    [JsonProperty("CardToTrade")]
    public Guid CardToTrade { get; set; }

    [JsonProperty("Type")]
    public string Type { get; set; }

    [JsonProperty("MinimumDamage")]
    public float MinimumDamage { get; set; }

    [JsonIgnore]
    public int UserId { get; set; }
}
