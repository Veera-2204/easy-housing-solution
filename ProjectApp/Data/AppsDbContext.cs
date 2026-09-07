using Microsoft.EntityFrameworkCore;
using ProjectApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace ProjectApp.Data
{
    /// <summary>
    /// The class AppsDbContext is used to create a Database Context for the EntityFrameworkCore to access the database
    /// </summary>
    public class AppsDbContext : IdentityDbContext<IdentityUser, IdentityRole, string>
    {
        private readonly ILogger<AppsDbContext> _logger;

        public AppsDbContext(DbContextOptions<AppsDbContext> options, ILogger<AppsDbContext> logger) : base(options)
        {
            _logger = logger;
        }

        
        public DbSet<Seller> Sellers { get; set; }
        public DbSet<Buyer> Buyers { get; set; }
        public DbSet<Property> Properties { get; set; }
        public DbSet<Image> Images { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<State> States { get; set; }
        public DbSet<City> Cities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)  // called only when migration is first created
        {
            base.OnModelCreating(modelBuilder);

            _logger.LogInformation("Configuring the model for AppsDbContext");


            // Configuration for Seller and Property relationship
            modelBuilder.Entity<Property>()
                .HasOne(p => p.Seller)
                .WithMany(s => s.Properties)
                .HasForeignKey(p => p.SellerId);

            // Configuration for another relationship, with State
            modelBuilder.Entity<Seller>()
                .HasOne(s => s.State)
                .WithMany() 
                .HasForeignKey(s => s.StateId)
                .OnDelete(DeleteBehavior.Restrict); 

            modelBuilder.Entity<Cart>()
               .HasOne(c => c.Buyer)
               .WithMany()
               .HasForeignKey(c => c.BuyerId)
               .OnDelete(DeleteBehavior.Cascade); // Allows cascade delete here

            modelBuilder.Entity<Cart>()
                .HasOne(c => c.Property)
                .WithMany()
                .HasForeignKey(c => c.PropertyId)
                .OnDelete(DeleteBehavior.Restrict); // Restricts related records to remain


            // Seed roles
            var adminRoleId = Guid.NewGuid().ToString();
            var adminRole = new IdentityRole
            {
                Id = adminRoleId,
                Name = "Admin",
                NormalizedName = "ADMIN"
            };

            // Check if role already exists
            var existingRole = modelBuilder.Model.FindEntityType(typeof(IdentityRole))
                .GetSeedData()
                .FirstOrDefault(r => r["Name"].ToString() == "Admin");

            if (existingRole == null)
            {
                modelBuilder.Entity<IdentityRole>().HasData(adminRole);
            }

            // Seed admin user
            var adminUserId = Guid.NewGuid().ToString();
            var adminUser = new IdentityUser
            {
                Id = adminUserId,
                UserName = "Admin",
                NormalizedUserName = "ADMIN",
                Email = "admin@easyhousing.com",
                NormalizedEmail = "ADMIN@EASYHOUSING.COM",
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString("D")
            };

            var hasher = new PasswordHasher<IdentityUser>();
            adminUser.PasswordHash = hasher.HashPassword(adminUser, "Admin@123");

            modelBuilder.Entity<IdentityUser>().HasData(adminUser);

            // Seed user-role association
            modelBuilder.Entity<IdentityUserRole<string>>().HasData(new IdentityUserRole<string>
            {
                UserId = adminUserId,
                RoleId = adminRoleId
            });

            // Seed State Names
            modelBuilder.Entity<State>().HasData(
            new State { StateId = 1, StateName = "Andhra Pradesh" },
            new State { StateId = 2, StateName = "Arunanchal Pradesh" },
            new State { StateId = 3, StateName = "Assam" },
            new State { StateId = 4, StateName = "Bihar" },
            new State { StateId = 5, StateName = "Chattisgarh" },
            new State { StateId = 6, StateName = "Goa" },
            new State { StateId = 7, StateName = "Gujarat" },
            new State { StateId = 8, StateName = "Haryana" },
            new State { StateId = 9, StateName = "Himachal Pradesh" },
            new State { StateId = 10, StateName = "Jharkhand" },
            new State { StateId = 11, StateName = "Karnataka" },
            new State { StateId = 12, StateName = "Kerala" },
            new State { StateId = 13, StateName = "Madhya Pradesh" },
            new State { StateId = 14, StateName = "Maharastra" },
            new State { StateId = 15, StateName = "Manipur" },
            new State { StateId = 16, StateName = "Meghalaya" },
            new State { StateId = 17, StateName = "Mizoram" },
            new State { StateId = 18, StateName = "Nagaland" },
            new State { StateId = 19, StateName = "Odisha" },
            new State { StateId = 20, StateName = "Punjab" },
            new State { StateId = 21, StateName = "Rajasthan" },
            new State { StateId = 22, StateName = "Sikkim" },
            new State { StateId = 23, StateName = "Tamil Nadu" },
            new State { StateId = 24, StateName = "Telangana" },
            new State { StateId = 25, StateName = "Tripura" },
            new State { StateId = 26, StateName = "Uttar Pradesh" },
            new State { StateId = 27, StateName = "Uttarakhand" },
            new State { StateId = 28, StateName = "West Bengal" },
            new State { StateId = 29, StateName = "Andaman and Nicobar Islands" },
            new State { StateId = 30, StateName = "Chandigarh" },
            new State { StateId = 31, StateName = "Dadra and Nagar Haveli" },
            new State { StateId = 32, StateName = "Daman and Diu" },
            new State { StateId = 33, StateName = "Lakshadweep" },
            new State { StateId = 34, StateName = "Delhi" },
            new State { StateId = 35, StateName = "Puducherry" }
            );

            _logger.LogInformation("Model configuration and seeding completed.");

        }
    }
}
