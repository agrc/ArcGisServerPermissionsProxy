using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using Raven.Client;
using Raven.Client.Document;

namespace ArcGisServerPermissionsProxy.Api.Controllers.Infrastructure
{
    public abstract class RavenApiController : ApiController
    {
        public static Lazy<IDocumentStore> DocumentStore = new Lazy<IDocumentStore>(() =>
        {
            var docStore = new DocumentStore
            {
                Url = "http://localhost:8080",
                DefaultDatabase = "Webinars"
            };
            docStore.Initialize();

            return docStore;
        });

        private IDocumentSession _session;
        public IAsyncDocumentSession asyncSession;

        public override async Task<HttpResponseMessage> ExecuteAsync(
            HttpControllerContext controllerContext,
            CancellationToken cancellationToken)
        {
            var result = await base.ExecuteAsync(controllerContext, cancellationToken);

            if (_session != null)
                _session.SaveChanges();
            
            if (asyncSession != null)
                await asyncSession.SaveChangesAsync();
            
            return result;
        }

        public IDocumentSession Session
        {
            get
            {
                if (asyncSession != null)
                    throw new NotSupportedException("Can't use both sync & async sessions in the same action");
                return _session ?? (_session = DocumentStore.Value.OpenSession());
            }
            set { _session = value; }
        }

        public IAsyncDocumentSession AsyncSession
        {
            get
            {
                if (_session != null)
                    throw new NotSupportedException("Can't use both sync & async sessions in the same action");
                return asyncSession ?? (asyncSession = DocumentStore.Value.OpenAsyncSession());
            }
            set { asyncSession = value; }
        }
    }
}