using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LearnMVC.Models;

[Table("Transaction")]
public partial class Transaction
{
    [Key]
    [Column("ID")]
    public Guid Id { get; set; }

    [Column("UserID")]
    public Guid UserId { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Amount { get; set; }

    [StringLength(10)]
    public string Type { get; set; } = null!;
   
    [StringLength(20)]
    public string? Method { get; set; }

    public string? Note { get; set; }

    public DateTime DateTime { get; set; }
    public string ? Catalogue {get;set;}

    [ForeignKey("UserId")]
    [InverseProperty("Transactions")]
    public virtual User User { get; set; } = null!;
}
