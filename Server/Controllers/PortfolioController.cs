using Akka.Actor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Common.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Server.Actors;
using Server.Models;
using Messages;
using Server.Services;
using Common.HelperModels;
using NLog;
using FinanceManager.Server.Database;
using Financemanager.Server.Database.Domain;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiExplorerSettings(GroupName = "Portfolio", IgnoreApi = false)]
    [Authorize]
    public class PortfolioController : BaseController
    {
        private PortfolioService _portfolioService;
        private ILogger _log;


        public PortfolioController(IAuthService authService, PortfolioService portfolioService): base(authService)
        {
            _log = LogManager.GetCurrentClassLogger();
            _portfolioService = portfolioService;
        }

        [Route("")]
        [HttpGet]
        public async Task<PortfolioDto> PortfolioByUserId()
        {
            return await _portfolioService.GetPortfolioByUserId(UserId);
        }

        [HttpPost]
        public async Task<PortfolioDto> AddToPortfolio([FromBody]AddToPortfolioDto purchase)
        {
           return await _portfolioService.AddPurchaseToPortfolio(UserId, purchase);
        }

        [HttpPost]
        [Route("purchases/{purchaseid}")]
        public async Task<PortfolioDto> UpdatePurchase(int purchaseid, [FromBody] StockPurchaseDto purchase)
        {
            return await _portfolioService.UpdatePurchase(UserId, purchase);
        }

        [Route("{portfolioId}/positions/{id}")]
        [HttpDelete]
        [Authorize]
        public async Task<PortfolioDto> DeletePosition(int id, int portfolioId)
        {
            return await _portfolioService.DeletePosition(id, portfolioId, UserId);
        }

        [HttpPost]
        [Route("uploadibscv")]
        public async Task<PortfolioDto> UploadIbPortfolioCsv([FromBody] FileUpload fileUpload) 
        {
            return await _portfolioService.UploadUsersIbPortfolio(UserId, fileUpload);
        }

    }


}
