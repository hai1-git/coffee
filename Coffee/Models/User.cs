using System;
using System.Collections.Generic;

namespace Coffee.Models;

public partial class User
{
    public int UserId { get; set; }

    public string? UserName { get; set; }

    public string? Email { get; set; }

    public string? Password { get; set; }

    public string? Phone { get; set; }

    public string? Address { get; set; }

    public int? RoleId { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsLocked { get; set; }

    public string? LockReason { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Cart> Carts { get; set; } = new List<Cart>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual Role? Role { get; set; }
}
