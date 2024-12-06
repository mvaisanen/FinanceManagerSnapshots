using Financemanager.Server.Database.Domain;
using FinanceManager.Server.Database;
using FinanceManager.Server.Database.Domain.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace FinanceManager.Server.IntegrationTests
{
    internal class TestDatabaseSeeder
    {
        FinanceManagerContext _fmCtx;
        AuthDbContext _authCtx;
        private UserManager<AppUser> _userManager;
        private RoleManager<IdentityRole> _roleManager;

        public TestDatabaseSeeder(FinanceManagerContext fmCtx, AuthDbContext authCtx, UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager) 
        {
            _fmCtx = fmCtx;
            _authCtx = authCtx;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task Seed()
        {
            System.Diagnostics.Debug.WriteLine("Seeding time!");

            AppUser testUser = null;
            AppUser adminUser = null;

            //Add roles
            var userRole = new IdentityRole("User");
            await _roleManager.CreateAsync(userRole);
            await _roleManager.AddClaimAsync(userRole, new System.Security.Claims.Claim(ClaimTypes.Role, "User"));
            var adminRole = new IdentityRole("Admin");
            await _roleManager.CreateAsync(adminRole);
            await _roleManager.AddClaimAsync(adminRole, new System.Security.Claims.Claim(ClaimTypes.Role, "Admin"));
            await _roleManager.AddClaimAsync(adminRole, new System.Security.Claims.Claim(ClaimTypes.Role, "User")); //admin is also a user in this case

            //Add one user and one admin (admin is also a user)
            adminUser = new AppUser() { UserName = "Administrator" };
            var result = await _userManager.CreateAsync(adminUser, "1admin2");
            await _userManager.AddToRoleAsync(adminUser, adminRole.Name);
            testUser = new AppUser() { UserName = "test" };
            var res = await _userManager.CreateAsync(testUser, "1test2");
            await _userManager.AddToRoleAsync(testUser, userRole.Name);

            await _authCtx.SaveChangesAsync();

            var portfolio1 = new Portfolio(testUser.Id);
            _fmCtx.Portfolios.Add(portfolio1);

            var watchlist = new Watchlist(testUser.Id);
            _fmCtx.Watchlists.Add(watchlist);

            var cvx = new Stock("CVX", "Chevron", 155.55, 6.04, 27, 18.52, Common.Currency.USD, Common.Exchange.NyseNasdaq, Common.DataUpdateSource.Manual, "Energy", DateTime.UtcNow);
            _fmCtx.Stocks.Add(cvx);
            
            await _fmCtx.SaveChangesAsync();
        }
    }
}
