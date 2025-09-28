using System.ComponentModel.DataAnnotations;

namespace Ava.Xioa.Common.Models;

public class SystemDbConfig
{
    [Required] public string LiteDbName { get; init; }
}