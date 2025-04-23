using System.Data;
using System.Data.SqlClient;
using Dapper;
using HomeCentre.PIM.Api.Controllers.api;
using HomeCentre.PIM.BAL.Contracts;
using HomeCentre.PIM.BAL.Model.Account;
using HomeCentre.PIM.BAL.Model.Geo;
using HomeCentre.PIM.CommonWeb;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace HomeCentre.PIM.Test
{


    //[TestFixture]
    public class DapperTests
    {
        private readonly IConfiguration configuration;


        public DapperTests(IConfiguration _configuration)
        {
            configuration = _configuration;
        }

        //[Test]
        //public async Task GetAllActiveUsers_ReturnsActiveUsers()
        //{
        //    using (IDbConnection connection = new SqlConnection(configuration["AppDb"]))
        //    {
        //        // Arrange
        //        var expectedActiveUsersCount = 2;

        //        // Act
        //        var activeUsers = await connection.QueryAsync("SELECT * FROM appuser WHERE isactive = 1");
        //        var activeUsersList = activeUsers.ToList();

        //        // Assert
        //        Assert.That(activeUsersList.Count, Is.EqualTo(expectedActiveUsersCount));
        //    }
        //}

        //[TestMethod]
        //public void GetActiveUsers

    }

}