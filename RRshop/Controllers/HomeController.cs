﻿using Microsoft.AspNetCore.Mvc;
using RRshop.Models;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using RRshop.ViewModels;
using RRshop.Services;
using RRshop.Data;
using System.Text;

namespace RRshop.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly rrshopContext _context;
        private readonly INotifyService _notifyService;

        public HomeController(ILogger<HomeController> logger, rrshopContext context, INotifyService notifyService)
        {
            _logger = logger;
            _context = context;
            _notifyService = notifyService;
        }


        public async Task<IActionResult> Index()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            HomeViewModel viewModel = new HomeViewModel();

            viewModel.Categories = await _context.Categories.ToListAsync();
            var allProds = await _context.Prods.Include(p => p.Category).ToListAsync();


            for (int i = 0; i < allProds.Count; i++)
            {
                List<float> sizeValueList = new();
                List<Size> sizeList = await _context.Sizes.Where(s => s.ProdId == allProds[i].Id).ToListAsync();
                sizeList.ForEach(s => sizeValueList.Add(s.Size1));
                
                viewModel.DetailProds.Add(new DetailProdViewModel()
                {
                    Prod = allProds[i],
                    SizeList = sizeValueList
                });
            }
            
            stopwatch.Stop(); //processes in 25+- milliseconds 
            Console.WriteLine(nameof(Index) + " processing time: " + stopwatch.ElapsedMilliseconds);
            return View(viewModel);
        }
        
        
        public async Task<IActionResult> IndexFilter(string category)
        {
            HomeViewModel viewModel = new HomeViewModel();

            viewModel.Categories = await _context.Categories.ToListAsync();
            var allProds = await _context.Prods.Include(p => p.Category)
                .Where(p => p.Category.Title == category).ToListAsync();


            for (int i = 0; i < allProds.Count; i++)
            {
                List<float> sizeValueList = new();
                List<Size> sizeList = await _context.Sizes.Where(s => s.ProdId == allProds[i].Id).ToListAsync();
                sizeList.ForEach(s => sizeValueList.Add(s.Size1));
                
                viewModel.DetailProds.Add(new DetailProdViewModel()
                {
                    Prod = allProds[i],
                    SizeList = sizeValueList
                });
            }

            return View("Index", viewModel);
        }
        
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        
        
        public async Task<IActionResult> BuyProd(int id)
        {
            var model = await _context.Prods.Include(p => p.Category)
                .FirstAsync(p => p.Id == id);
            
            return View(new BuyProdViewModel()
            {
                Count = 1,
                Prod = model
            });
        }
        
        
        [HttpPost]
        public IActionResult UserCartAdd(int prodId, int count)
        {
            if (count <= 0) return RedirectToAction("Index");
            try
            {
                int userId = int.Parse(s: HttpContext.User.FindFirst("id").Value);

                for (int i = 0; i < count; i++)
                {
                    _context.Database.ExecuteSqlInterpolated($"INSERT INTO user_cart VALUES({userId}, {prodId})");
                }

                Notify(prodId, count, userId);

                return RedirectToAction("Index", "UserCart");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                ModelState.AddModelError("", e.Message);
                return Redirect(nameof(Index));
            }
        }

        private void Notify(int prodId, int count, int userId)
        {
            var info = new StringBuilder();
            var user = _context.Users.First(u => u.Id == userId);
            var prod = _context.Prods.
                Include(p => p.Category).
                First(p => p.Id == prodId);
            info.Append($"ПОКУПАТЕЛЬ \n{user.Name} ({user.Id})\nНомер: {user.Phone}\nМестопложение: {user.City}");
            info.Append($"\n\nТОВАР \n{prod.Title} ({prod.Id}) \nКатегория: {prod.Category.Title}" +
                           $"\nЦвет: {prod.Color}\nКоличество: {count}\nЦена: {prod.Price} руб." +
                           $"\nИТОГО: {prod.Price*count} рублей");
            
            _notifyService.Notify(info.ToString());
        }
    }
}