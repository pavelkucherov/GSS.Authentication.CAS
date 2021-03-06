﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using GSS.Authentication.CAS.Tests;
using Microsoft.Extensions.FileProviders;
using Microsoft.Owin.Testing;
using Moq;
using Owin;
using Xunit;

namespace GSS.Authentication.CAS.Owin.Tests
{
    public class CasSingleSignOutMiddlewareTest : IClassFixture<CasFixture>,IDisposable
    {
        private readonly IServiceTicketStore _store;
        private readonly TestServer _server;
        private readonly HttpClient _client;
        private readonly CasFixture _fixture;

        public CasSingleSignOutMiddlewareTest(CasFixture fixture)
        {
            _fixture = fixture;
            // Arrange
            _store = Mock.Of<IServiceTicketStore>();
            _server = TestServer.Create(app =>
            {
                app.UseCasSingleSignOut(new AuthenticationSessionStoreWrapper(_store));
            });
            _client = _server.HttpClient;
        }

        public void Dispose()
        {
            _server.Dispose();
        }

        [Trait("pass", "true")]
        [Fact]
        public async Task RecievedSignoutRequest_FailAsync()
        {
            // Act
            await _client.PostAsync("/", new StringContent(string.Empty));

            // Assert
            Mock.Get(_store).Verify(x => x.RemoveAsync(It.IsAny<string>()), Times.Never);
        }

        [Trait("pass", "true")]
        [Fact]
        public async Task RecievedSignoutRequest_SuccessAsync()
        {
            // Arrange
            var removedTicket = string.Empty;
            Mock.Get(_store).Setup(x => x.RemoveAsync(It.IsAny<string>()))
                .Callback<string>((x) => removedTicket = x)
                .Returns(Task.CompletedTask);
            var ticket = Guid.NewGuid().ToString();
            var parts = new Dictionary<string, string>
            {
                ["logoutRequest"] = _fixture.FileProvider.ReadAsString("SamlLogoutRequest.xml").Replace("$TICKET", ticket)
            };

            // Act
            await _client.PostAsync("/", new FormUrlEncodedContent(parts));

            // Assert
            Assert.Equal(ticket, removedTicket);
            Mock.Get(_store).Verify(x => x.RemoveAsync(ticket), Times.Once);
        }
    }
}
