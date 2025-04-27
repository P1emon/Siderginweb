using Microsoft.AspNetCore.Mvc;
using MyEStore.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace MyEStore.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class HomeController : Controller
    {
        private readonly MyeStoreContext _context;

        public HomeController(MyeStoreContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            // Get data for the last 30 days
            var startDate = DateTime.Now.AddDays(-30);

            // Calculate total units sold (tổng số lượng bán)
            var totalUnitsSold = _context.ChiTietHds
                .Where(ct => _context.HoaDons
                    .Any(hd => hd.MaHd == ct.MaHd && hd.NgayDat >= startDate))
                .Sum(ct => ct.SoLuong);

            // Calculate total revenue (tổng doanh thu)
            var totalRevenue = _context.ChiTietHds
                .Where(ct => _context.HoaDons
                    .Any(hd => hd.MaHd == ct.MaHd && hd.NgayDat >= startDate))
                .Sum(ct => ct.DonGia * ct.SoLuong);

            // Calculate average order value
            var totalOrders = _context.HoaDons
                .Count(hd => hd.NgayDat >= startDate);
            var averageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;

            // Calculate top products with daily sales data
            var topProducts = _context.HangHoas
                .Select(h => new
                {
                    h.TenHh,
                    h.MaHh,
                    UnitsSold = _context.ChiTietHds
                        .Where(ct => ct.MaHh == h.MaHh &&
                            _context.HoaDons.Any(hd => hd.MaHd == ct.MaHd && hd.NgayDat >= startDate))
                        .Sum(ct => ct.SoLuong)
                })
                .OrderByDescending(x => x.UnitsSold)
                .Take(10) // Top 10 products
                .ToList();

            var dailySales = _context.ChiTietHds
                .Where(ct => _context.HoaDons
                    .Any(hd => hd.MaHd == ct.MaHd && hd.NgayDat >= startDate))
                .Join(_context.HoaDons,
                    ct => ct.MaHd,
                    hd => hd.MaHd,
                    (ct, hd) => new { ct.MaHh, ct.SoLuong, hd.NgayDat })
                .Where(x => topProducts.Select(tp => tp.MaHh).Contains(x.MaHh))
                .GroupBy(x => new { x.MaHh, Date = x.NgayDat.Date })
                .Select(g => new
                {
                    g.Key.MaHh,
                    g.Key.Date,
                    UnitsSold = g.Sum(x => x.SoLuong)
                })
                .ToList();

            var strongSales = topProducts.Select(tp => new
            {
                tp.TenHh,
                tp.UnitsSold,
                Ratio = totalUnitsSold > 0 ? (tp.UnitsSold / (double)totalUnitsSold) * 100 : 0,
                DailySales = Enumerable.Range(0, 30)
                    .Select(i => startDate.AddDays(i).Date)
                    .Select(date => new
                    {
                        Date = date,
                        UnitsSold = dailySales
                            .Where(ds => ds.MaHh == tp.MaHh && ds.Date == date)
                            .Sum(ds => ds.UnitsSold)
                    })
                    .ToList()
            }).ToList();

            // Calculate weak sales (sức mua yếu)
            var weakSales = _context.HangHoas
                .GroupJoin(
                    _context.ChiTietHds
                        .Where(ct => _context.HoaDons
                            .Any(hd => hd.MaHd == ct.MaHd && hd.NgayDat >= startDate)),
                    h => h.MaHh,
                    ct => ct.MaHh,
                    (h, ctGroup) => new
                    {
                        h.TenHh,
                        h.SoLuong, // Current stock (tồn kho)
                        UnitsSold = ctGroup.Sum(ct => ct.SoLuong) // Số lượng bán
                    })
                .OrderBy(x => x.UnitsSold) // Ascending to get least sold
                .Take(10) // Bottom 10 products
                .ToList();

            // Generate restock forecast (dự báo nhập hàng)
            var restockForecast = _context.HangHoas
                .Select(h => new
                {
                    h.TenHh,
                    h.SoLuong, // Current stock (tồn kho)
                    UnitsSold = _context.ChiTietHds
                        .Where(ct => ct.MaHh == h.MaHh &&
                            _context.HoaDons.Any(hd => hd.MaHd == ct.MaHd && hd.NgayDat >= startDate))
                        .Sum(ct => ct.SoLuong)
                })
                .Where(x => x.SoLuong < 50 && x.UnitsSold > 0) // Low stock and has sales
                .OrderByDescending(x => x.UnitsSold)
                .Take(5) // Top 5 to restock
                .ToList();

            // Calculate customer purchase ratio by product
            var customerPurchaseRatios = GetCustomerPurchaseRatios(startDate);

            // Calculate inventory turnover rate
            var inventoryTurnoverRates = GetInventoryTurnoverRates(startDate);

            // Get categories (Loai) for the category selector dropdown
            var danhSachLoai = _context.Loais.ToList();

            // Prepare ViewModel
            var viewModel = new DashboardViewModel
            {
                TotalUnitsSold = totalUnitsSold,
                TotalRevenue = totalRevenue,
                AverageOrderValue = averageOrderValue,
                TotalOrders = totalOrders,
                StrongSales = strongSales
                    .Select(x => new StrongSale
                    {
                        ProductName = x.TenHh,
                        UnitsSold = x.UnitsSold, // Số lượng bán
                        Ratio = x.Ratio,
                        DailySales = x.DailySales
                            .Select(ds => new DailySale
                            {
                                Date = ds.Date,
                                UnitsSold = ds.UnitsSold
                            })
                            .ToList()
                    })
                    .ToList(),
                WeakSales = weakSales
                    .Select(x => new WeakSale
                    {
                        ProductName = x.TenHh,
                        UnitsSold = x.UnitsSold, // Số lượng bán
                        CurrentStock = x.SoLuong // Tồn kho
                    })
                    .ToList(),
                RestockForecast = restockForecast
                    .Select(x => new RestockItem
                    {
                        ProductName = x.TenHh,
                        CurrentStock = x.SoLuong, // Tồn kho
                        UnitsSold = x.UnitsSold // Số lượng bán
                    })
                    .ToList(),
                CustomerPurchaseRatios = customerPurchaseRatios,
                InventoryTurnoverRates = inventoryTurnoverRates
            };

            // Add categories list to ViewBag to use in the view
            ViewBag.DanhSachLoai = danhSachLoai;

            return View(viewModel);
        }

        private List<CustomerPurchaseRatio> GetCustomerPurchaseRatios(DateTime startDate)
        {
            // Get top 10 products
            var topProducts = _context.HangHoas
                .Select(h => new
                {
                    h.MaHh,
                    h.TenHh,
                    TotalSales = _context.ChiTietHds
                        .Where(ct => ct.MaHh == h.MaHh &&
                            _context.HoaDons.Any(hd => hd.MaHd == ct.MaHd && hd.NgayDat >= startDate))
                        .Sum(ct => ct.SoLuong)
                })
                .OrderByDescending(p => p.TotalSales)
                .Take(10)
                .ToList();

            // Get total number of customers who made purchases in the period
            var totalCustomers = _context.HoaDons
                .Where(hd => hd.NgayDat >= startDate)
                .Select(hd => hd.MaKh)
                .Distinct()
                .Count();

            var result = new List<CustomerPurchaseRatio>();

            foreach (var product in topProducts)
            {
                // Number of unique customers who bought this product
                var customersWhoBoughtProduct = _context.HoaDons
                    .Where(hd => hd.NgayDat >= startDate)
                    .Join(_context.ChiTietHds,
                        hd => hd.MaHd,
                        ct => ct.MaHd,
                        (hd, ct) => new { hd.MaKh, ct.MaHh })
                    .Where(x => x.MaHh == product.MaHh)
                    .Select(x => x.MaKh)
                    .Distinct()
                    .Count();

                // Calculate the percentage of customers who bought this product
                var ratio = totalCustomers > 0 ? (double)customersWhoBoughtProduct / totalCustomers * 100 : 0;

                result.Add(new CustomerPurchaseRatio
                {
                    ProductName = product.TenHh,
                    CustomersCount = customersWhoBoughtProduct,
                    Ratio = Math.Round(ratio, 2)
                });
            }

            return result;
        }

        private List<InventoryTurnoverRate> GetInventoryTurnoverRates(DateTime startDate)
        {
            // Get top products for inventory turnover analysis
            var topProducts = _context.HangHoas
                .Select(h => new
                {
                    h.MaHh,
                    h.TenHh,
                    h.SoLuong, // Current stock
                    UnitsSold = _context.ChiTietHds
                        .Where(ct => ct.MaHh == h.MaHh &&
                            _context.HoaDons.Any(hd => hd.MaHd == ct.MaHd && hd.NgayDat >= startDate))
                        .Sum(ct => ct.SoLuong)
                })
                .Where(p => p.UnitsSold > 0) // Only include products with sales
                .OrderByDescending(p => p.UnitsSold)
                .Take(10)
                .ToList();

            var result = new List<InventoryTurnoverRate>();

            // Number of days in period (30 days)
            int daysInPeriod = 30;

            foreach (var product in topProducts)
            {
                // Get the average inventory level (current stock + estimated beginning stock)
                var estimatedBeginningStock = product.SoLuong + product.UnitsSold;
                var averageInventory = (product.SoLuong + estimatedBeginningStock) / 2.0;

                // Calculate daily sales rate
                var dailySalesRate = product.UnitsSold / (double)daysInPeriod;

                // Calculate days of supply (how many days the current stock will last)
                var daysOfSupply = dailySalesRate > 0 ? product.SoLuong / dailySalesRate : 0;

                // Calculate inventory turnover rate (how many times the inventory is sold in the period)
                var turnoverRate = averageInventory > 0 ? product.UnitsSold / averageInventory : 0;

                // Calculate annualized turnover rate (for comparison)
                var annualizedTurnoverRate = turnoverRate * (365.0 / daysInPeriod);

                result.Add(new InventoryTurnoverRate
                {
                    ProductName = product.TenHh,
                    CurrentStock = product.SoLuong,
                    UnitsSold = product.UnitsSold,
                    DailySalesRate = Math.Round(dailySalesRate, 2),
                    DaysOfSupply = Math.Round(daysOfSupply, 1),
                    TurnoverRate = Math.Round(turnoverRate, 2),
                    AnnualizedTurnoverRate = Math.Round(annualizedTurnoverRate, 2)
                });
            }

            return result;
        }

        [HttpGet]
        public IActionResult GetProductsByCategory(int categoryId)
        {
            var startDate = DateTime.Now.AddDays(-30);

            var products = _context.HangHoas
                .Where(h => h.MaLoai == categoryId)
                .Select(h => new
                {
                    productName = h.TenHh,
                    unitsSold = _context.ChiTietHds
                        .Where(ct => ct.MaHh == h.MaHh &&
                            _context.HoaDons.Any(hd => hd.MaHd == ct.MaHd && hd.NgayDat >= startDate))
                        .Sum(ct => ct.SoLuong),
                    currentStock = h.SoLuong // Current stock
                })
                .OrderByDescending(x => x.unitsSold)
                .ToList();

            return Json(products);
        }

        [HttpGet]
        public IActionResult GetCustomerPurchaseRatios()
        {
            var startDate = DateTime.Now.AddDays(-30);
            var ratios = GetCustomerPurchaseRatios(startDate);
            return Json(ratios);
        }

        [HttpGet]
        public IActionResult GetInventoryAnalytics()
        {
            var startDate = DateTime.Now.AddDays(-30);
            var turnoverRates = GetInventoryTurnoverRates(startDate);
            return Json(turnoverRates);
        }
    }

    public class DashboardViewModel
    {
        public int TotalUnitsSold { get; set; } // Tổng số lượng bán
        public double TotalRevenue { get; set; } // Tổng doanh thu
        public double AverageOrderValue { get; set; } // Giá trị đơn hàng trung bình
        public int TotalOrders { get; set; } // Tổng số đơn hàng
        public List<StrongSale> StrongSales { get; set; } // Sức mua mạnh
        public List<WeakSale> WeakSales { get; set; } // Sức mua yếu
        public List<RestockItem> RestockForecast { get; set; } // Dự báo nhập hàng
        public List<CustomerPurchaseRatio> CustomerPurchaseRatios { get; set; } // Tỉ xuất mua hàng của khách hàng
        public List<InventoryTurnoverRate> InventoryTurnoverRates { get; set; } // Tỉ lệ luân chuyển hàng tồn kho
    }

    public class StrongSale
    {
        public string ProductName { get; set; } // Tên sản phẩm
        public int UnitsSold { get; set; } // Số lượng bán
        public double Ratio { get; set; } // Tỉ lệ mua
        public List<DailySale> DailySales { get; set; } // Doanh số hàng ngày
    }

    public class DailySale
    {
        public DateTime Date { get; set; } // Ngày
        public int UnitsSold { get; set; } // Số lượng bán
    }

    public class WeakSale
    {
        public string ProductName { get; set; } // Tên sản phẩm
        public int UnitsSold { get; set; } // Số lượng bán
        public int CurrentStock { get; set; } // Tồn kho
    }

    public class RestockItem
    {
        public string ProductName { get; set; } // Tên sản phẩm
        public int CurrentStock { get; set; } // Tồn kho
        public int UnitsSold { get; set; } // Số lượng bán
    }

    public class ProductSale
    {
        public string ProductName { get; set; }
        public int UnitsSold { get; set; }
        public int CurrentStock { get; set; }
    }

    public class CustomerPurchaseRatio
    {
        public string ProductName { get; set; } // Tên sản phẩm
        public int CustomersCount { get; set; } // Số lượng khách hàng đã mua
        public double Ratio { get; set; } // Tỉ lệ % khách hàng đã mua
    }

    public class InventoryTurnoverRate
    {
        public string ProductName { get; set; } // Tên sản phẩm
        public int CurrentStock { get; set; } // Tồn kho hiện tại
        public int UnitsSold { get; set; } // Số lượng đã bán
        public double DailySalesRate { get; set; } // Tỉ lệ bán hàng ngày
        public double DaysOfSupply { get; set; } // Số ngày tồn kho còn đủ cung cấp
        public double TurnoverRate { get; set; } // Tỉ lệ luân chuyển trong 30 ngày
        public double AnnualizedTurnoverRate { get; set; } // Tỉ lệ luân chuyển quy đổi theo năm
    }
}