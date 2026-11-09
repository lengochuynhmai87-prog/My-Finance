using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LearnMVC.Models;

[Table("FinanceSetting")]
public partial class FinanceSetting
{
    [Key]
    [Column("ID")]
    public Guid Id { get; set; }

    [Column("UserID")]
    [StringLength(50)]
    public Guid UserId { get; set; }

    public int InitialBalance { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("FinanceSettings")]
    public virtual User User { get; set; } = null!;
}
