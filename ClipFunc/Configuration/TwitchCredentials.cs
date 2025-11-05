using System.ComponentModel.DataAnnotations;

namespace ClipFunc.Configuration;

public sealed class TwitchCredentials
{
    [Required(AllowEmptyStrings = false), RegularExpression("^[a-z0-9]{30}$")]
    public required string ClientId { get; set; }

    [Required(AllowEmptyStrings = false), RegularExpression("^[a-z0-9]{30}$")]
    public required string ClientSecret { get; set; }
}