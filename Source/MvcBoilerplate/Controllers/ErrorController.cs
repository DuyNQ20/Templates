﻿namespace MvcBoilerplate.Controllers
{
    using System.Net;
    using System.Web.Mvc;
    using System.Web.UI;
    using MvcBoilerplate.Constants;
    using MvcBoilerplate.Models;

    /// <summary>
    /// Provides methods that respond to HTTP requests with HTTP errors.
    /// </summary>
    [RoutePrefix("error")]
    public sealed class ErrorController : Controller
    {
        #region Public Methods

        /// <summary>
        /// Returns a HTTP 404 Not Found error view. Returns a partial view if the request is an AJAX call.
        /// </summary>
        /// <returns>The partial or full not found view.</returns>
        [OutputCache(Duration = 60 * 60 * 24, Location = OutputCacheLocation.Any)]
        [Route("notfound", Name = ErrorControllerRoute.GetNotFound)]
        public ActionResult NotFound()
        {
            return this.GetErrorView(HttpStatusCode.NotFound, ErrorControllerAction.NotFound);
        }

        /// <summary>
        /// Returns a HTTP 401 Unauthorized error view. Returns a partial view if the request is an AJAX call.
        /// </summary>
        /// <returns>The partial or full unauthorized view.</returns>
        [OutputCache(Duration = 60 * 60 * 24, Location = OutputCacheLocation.Any)]
        [Route("unauthorized", Name = ErrorControllerRoute.GetUnauthorized)]
        public ActionResult Unauthorized()
        {
            return this.GetErrorView(HttpStatusCode.Unauthorized, ErrorControllerAction.Unauthorized);
        } 

        #endregion

        #region Private Methods

        private ActionResult GetErrorView(HttpStatusCode statusCode, string viewName)
        {
            this.Response.StatusCode = (int)HttpStatusCode.NotFound;

            // Don't show IIS custom errors.
            this.Response.TrySkipIisCustomErrors = true;

            ErrorModel error = new ErrorModel()
            {
                RequestedUrl = this.Request.Url.ToString(),
                ReferrerUrl =
                    (this.Request.UrlReferrer == null) ?
                    null :
                    this.Request.UrlReferrer.ToString()
            };

            ActionResult result;
            if (this.Request.IsAjaxRequest())
            {
                // This allows us to show not found errors even in partial views.
                result = this.PartialView(viewName, error);
            }
            else
            {
                result = this.View(viewName, error);
            }

            return result;
        }

        #endregion
    }
}