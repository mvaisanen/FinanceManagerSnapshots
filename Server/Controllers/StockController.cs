using Akka.Actor;
using Messages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Common.Dtos;
using Common.HelperModels;
using NLog;
using Server.Actors;
using FinanceManager.Server.Database;
using Server.Models;
using Server.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiExplorerSettings(GroupName = "Stock", IgnoreApi = false)]
    [ApiController]
    public class StockController: ControllerBase
    {
        private IActorRef _stockActor;
        private AuthDbContext _authCtx;
        private StockService _stockService;
        private ILogger _log;

        public StockController(StockActorProvider sProvider, AuthDbContext authCtx, StockService stockService)
        {
            _log = LogManager.GetCurrentClassLogger();
            _stockActor = sProvider.Invoke();
            _authCtx = authCtx;
            _stockService = stockService;
        }

        [HttpGet]
        [Route("matches/{param}")]
        public async Task<List<StockDTO>> ClosestStockMatches(string param)
        {
            var res = await _stockActor.Ask<DbOperationResult<List<StockDTO>>>(StockMessage.NewGetClosestMatches(param));
            if (res.Succeeded)
                return res.Item;
            throw new AkkaError(500, res.ErrorMsg);
        }


        [HttpPost]
        [Authorize(Roles = "Admin")]
        [Route("uploadusccc")]
        public async Task<IActionResult> UploadUsCccList([FromForm] IFormFileCollection files)
        {
            string userId = "";
            var user = _authCtx.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
            if (user == null)
            {
                _log.Debug($"User was not found! From context: User={User.Identity.Name}");
                throw new UnauthorizedAccessException("Invalid User");
            }
            userId = user.Id;

            var fCount = Request.Form.Files.Count;
            Console.WriteLine("Files: " + fCount);

            var file = Request.Form.Files[0];
            if (file.Length > 0)
            {
                string filename = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');
                if (filename.Contains("\\"))
                    filename = filename.Substring(filename.LastIndexOf("\\") + 1);
                var fileWithPath = $"{AppDomain.CurrentDomain.BaseDirectory}Uploads\\{filename}";
                using (var stream = System.IO.File.Create(fileWithPath))
                {
                    await file.CopyToAsync(stream);
                }
                await _stockService.UploadUsCcc(fileWithPath);
                return Ok();
            }
            else
            {
                return BadRequest("File is empty");
            }



            /*
            foreach (IFormFile fileUpload in files)
            {
                if (fileUpload.Length > 0)
                {
                    using (var stream = System.IO.File.Create("UploadedFormFile.xlsx"))
                    {
                        await fileUpload.CopyToAsync(stream);
                    }
                }
            }*/

            //await _stockService.UploadUsCcc(fileUpload);
           
        }

        [HttpGet]
        [Route("dividendhistory/{stockId}")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IEnumerable<HistoricalDividendDto>> GetDividendHistory(int stockId)
        {

            return  _stockService.GetDividendHistory(stockId);
        }

        [HttpGet]
        [Route("pricehistory/{stockId}")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IEnumerable<HistoricalPriceDto>> GetPriceHistory(int stockId)
        {

            return _stockService.GetPriceHistory(stockId);
        }


        /*[HttpPost]
        [Authorize(Roles = "Admin")]
        [Route("{ticker}/dividendhistory")]
        public async Task<IActionResult> UploadDividendHistory(string ticker, [FromForm] IFormFileCollection files)
        {
            string userId = "";
            var user = _authCtx.Users.FirstOrDefault(u => u.UserName == User.Identity.Name);
            if (user == null)
            {
                _log.Debug($"User was not found! From context: User={User.Identity.Name}");
                throw new UnauthorizedAccessException("Invalid User");
            }
            userId = user.Id;

            var fCount = Request.Form.Files.Count;
            Console.WriteLine("Files: " + fCount);

            var file = Request.Form.Files[0];
            if (file.Length > 0)
            {
                List<string> fileRows = new List<string>();
                using (var reader = new StreamReader(file.OpenReadStream()))
                {
                    while (reader.Peek() >= 0)
                        fileRows.Add(await reader.ReadLineAsync());
                }
                
                await _stockService.UploadDividendHistory(fileRows);
                return Ok();
            }
            else
            {
                return BadRequest("File is empty");
            }
        }*/

    }
}
