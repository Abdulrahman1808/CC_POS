using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using POSSystem.Data.Interfaces;
using POSSystem.Models;

namespace POSSystem.Data;

/// <summary>
/// Seeds the database with sample data for development/testing.
/// Only runs in DEBUG builds.
/// </summary>
public static class DataSeeder
{
    /// <summary>
    /// Seeds sample products if the database is completely empty (no products at all, including deleted).
    /// DISABLED: No mock data - production clean start.
    /// </summary>
    public static async Task SeedIfEmptyAsync(IDataService dataService, AppDbContext context)
    {
        // Seeding disabled - users should add their own products
        Debug.WriteLine("[Seeder] Data seeding is disabled - clean production start.");
        await Task.CompletedTask;
        return;
        
        // Original DEBUG-only seeding code removed
    }

    private static List<Product> GetSampleProducts()
    {
        return new List<Product>
        {
            // Coffee
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Espresso",
                Sku = "COF-001",
                Category = "Coffee",
                Price = 3.50m,
                StockQuantity = 100,
                IsActive = true
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Latte",
                Sku = "COF-002",
                Category = "Coffee",
                Price = 4.50m,
                StockQuantity = 100,
                IsActive = true
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Cappuccino",
                Sku = "COF-003",
                Category = "Coffee",
                Price = 4.00m,
                StockQuantity = 100,
                IsActive = true
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Americano",
                Sku = "COF-004",
                Category = "Coffee",
                Price = 3.00m,
                StockQuantity = 100,
                IsActive = true
            },
            
            // Pastries
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Croissant",
                Sku = "PST-001",
                Category = "Pastries",
                Price = 2.75m,
                StockQuantity = 50,
                IsActive = true
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Chocolate Muffin",
                Sku = "PST-002",
                Category = "Pastries",
                Price = 3.25m,
                StockQuantity = 40,
                IsActive = true
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Blueberry Scone",
                Sku = "PST-003",
                Category = "Pastries",
                Price = 3.00m,
                StockQuantity = 30,
                IsActive = true
            },
            
            // Drinks
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Hot Chocolate",
                Sku = "DRK-001",
                Category = "Drinks",
                Price = 3.75m,
                StockQuantity = 80,
                IsActive = true
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Iced Tea",
                Sku = "DRK-002",
                Category = "Drinks",
                Price = 2.50m,
                StockQuantity = 60,
                IsActive = true
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Fresh Orange Juice",
                Sku = "DRK-003",
                Category = "Drinks",
                Price = 4.25m,
                StockQuantity = 25,
                IsActive = true
            }
        };
    }

    /// <summary>
    /// Creates a random test transaction for stress-testing sync.
    /// </summary>
    public static async Task<Transaction?> CreateTestTransactionAsync(IDataService dataService)
    {
#if DEBUG
        try
        {
            var products = (await dataService.GetAllProductsAsync()).ToList();
            if (products.Count < 3)
            {
                Debug.WriteLine("[Test] Not enough products for test transaction");
                return null;
            }

            var random = new Random();
            var selectedProducts = products.OrderBy(_ => random.Next()).Take(3).ToList();

            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                PaymentMethod = random.Next(2) == 0 ? "Cash" : "Card",
                Items = selectedProducts.Select(p => new TransactionItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = p.Id,
                    ProductName = p.Name,
                    UnitPrice = p.Price,
                    Quantity = random.Next(1, 4)
                }).ToList()
            };

            var success = await dataService.CreateTransactionAsync(transaction);
            
            if (success)
            {
                Debug.WriteLine($"[Test] ✓ Created test transaction #{transaction.Id.ToString()[..8]} with {transaction.Items.Count} items, Total: {transaction.Total:C2}");
                return transaction;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Test] Error creating transaction: {ex.Message}");
            return null;
        }
#else
        return null;
#endif
    }
}
