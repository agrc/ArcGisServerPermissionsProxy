using System;
using System.Web.Mvc;
using Ninject;
using Raven.Client;

namespace ArcGisServerPermissionsProxy.Api.Controllers.Infrastructure
{
    public abstract class RavenController : Controller
    {
        private IDocumentSession _session;
        private IAsyncDocumentSession _asyncSession;
        private string _database;
        public string Database
        {
            get { return string.IsNullOrEmpty(_database) ? null : "app_" + _database.ToLowerInvariant(); }
            set { _database = value; }
        }

        [Inject]
        public IDocumentStore DocumentStore { get; set; }

        public IDocumentSession DbSession
        {
            get
            {
                if (_asyncSession != null)
                    throw new NotSupportedException("Can't use both sync & async sessions in the same action");
                return _session ?? (_session = DocumentStore.OpenSession(Database));
            }
            set { _session = value; }
        }

        public IAsyncDocumentSession AsyncSession
        {
            get
            {
                if (_session != null)
                    throw new NotSupportedException("Can't use both sync & async sessions in the same action");
                return _asyncSession ?? (_asyncSession = DocumentStore.OpenAsyncSession(Database));
            }
            set { _asyncSession = value; }
        }
    }
}