using Common.Mediatr.Helpers;
using Common.Mediatr.Model;
using Core.Common.Types;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using FileResult = Core.Common.Types.FileResult;

namespace Core.Api
{
    public class CoreApiController : ControllerBase
    {
		public ActionResult<T> FiltrarRetornoArquivo<T>(Response<T> response) where T : FileResult
		{
			if (VerificarObjetoBase(response))
			{
				return BadRequest();
			}

			if (response.Output != null)
			{
				var fileStream = new FileStream(response.Output.FullPath, FileMode.Open, FileAccess.Read);
				FileStreamResult filtrarRetornoArquivo = File(fileStream, response.Output.ContentType, response.Output.OriginalName);
				return filtrarRetornoArquivo;
			}

			return ValidarErro(response);
		}

		public ActionResult<List<T>> FiltrarRetorno<T>(Response<PagedResult<T>> response)
		{
			if (VerificarObjetoBase(response))
			{
				return BadRequest();
			}

			if (response.Output != null)
			{
				return ClassificarOk(response);
			}

			return ValidarErro(response);
		}

		public ActionResult<List<T>> FiltrarRetorno<T>(Response<List<T>> response)
		{
			if (VerificarObjetoBase(response))
			{
				return BadRequest();
			}

			if (response.Output != null)
			{
				if (response.Output.Count == 0)
				{
					return NoContent();
				}

				return PrepareOk(response);
			}

			return ValidarErro(response);
		}

		public ActionResult<T> FiltrarRetorno<T>(Response<T> response)
		{
			if (VerificarObjetoBase(response))
			{
				return BadRequest();
			}

			return response.Output != null ? PrepareOk(response) : ValidarErro(response);
		}

		private bool VerificarObjetoBase<T>(Response<T> response)
		{
			if (response == null || (response.Output == null && response.Notifications == null))
			{
				return true;
			}

			return false;
		}

		private ActionResult ValidarErro<T>(Response<T> response)
		{
			if (response.Notifications == null || response.Notifications.Count == 0)
			{
				return BadRequest();
			}

			var max = response.Notifications.GroupBy(a => a.Effect).Max(a => a.Key);
			var msgs = response.Notifications;

			switch (max)
			{
				case Effect.Conflicted:
					return Conflict(msgs);

				case Effect.NotAuthorized:
					return StatusCode((int)HttpStatusCode.Forbidden, msgs);

				case Effect.NotImplemented:
					return StatusCode((int)HttpStatusCode.NotImplemented, msgs);

				case Effect.PreConditionFailed:
					return StatusCode((int)HttpStatusCode.PreconditionFailed, msgs);

				case Effect.NotFound:
					return NotFound(msgs);

				case Effect.Error:
					return StatusCode((int)HttpStatusCode.UnprocessableEntity, msgs);

				case Effect.Validation:
				case Effect.InvalidStatus:
					return BadRequest(msgs);

				default:
					throw new NotSupportedException("Effect no defined");
			}
		}

		private ActionResult<List<T>> ClassificarOk<T>(Response<PagedResult<T>> response)
		{
			if (response.Output.IsEmpty)
			{
				return NoContent();
			}

			// 206
			Response.Headers.Add("Link", GetLinkHeader(response.Output));
			Response.Headers.Add("X-Total-Count", response.Output.TotalResults.ToString());
			Response.Headers.Add("Content-Range", GetContentRange(response.Output));
			Response.StatusCode = 206;
			return StatusCode(206, response.Output.Items);
		}

		private StringValues GetContentRange<T>(PagedResult<T> responseOutput)
		{
			int rangeStart = ((responseOutput.CurrentPage - 1) * responseOutput.ResultsPerPage) + 1;
			long rangeEnd = responseOutput.CurrentPage * responseOutput.ResultsPerPage;
			rangeEnd = rangeEnd > responseOutput.TotalResults ? responseOutput.TotalResults : rangeEnd;
			return $" {typeof(T).Name} {rangeStart}-{rangeEnd}/{responseOutput.TotalResults}";
		}

		private string GetLinkHeader(PagedResultBase result)
		{
			var first = GetPageLink(result.CurrentPage, 1);
			var last = GetPageLink(result.CurrentPage, result.TotalPages);
			var prev = string.Empty;
			var next = string.Empty;
			if (result.CurrentPage > 1 && result.CurrentPage <= result.TotalPages)
			{
				prev = GetPageLink(result.CurrentPage, result.CurrentPage - 1);
			}
			if (result.CurrentPage < result.TotalPages)
			{
				next = GetPageLink(result.CurrentPage, result.CurrentPage + 1);
			}

			return $"{FormatLink(next, "next")}{FormatLink(last, "last")}" +
				   $"{FormatLink(first, "first")}{FormatLink(prev, "prev")}";
		}

		private string GetPageLink(int currentPage, int page)
		{
			const string pageLink = "pagina";
			var path = Request.Path.HasValue ? Request.Path.ToString() : string.Empty;
			var queryString = Request.QueryString.HasValue ? Request.QueryString.ToString() : string.Empty;
			var conjunction = string.IsNullOrWhiteSpace(queryString) ? "?" : "&";
			var fullPath = $"{path}{queryString}";
			var pageArg = $"{pageLink}={page}";
			var link = fullPath.ToLower().Contains($"{pageLink}=")
				? fullPath.ToLower().Replace($"{pageLink}={currentPage}", pageArg)
				: (fullPath + $"{conjunction}{pageArg}");

			return link;
		}

		private static string FormatLink(string path, string rel)
			=> string.IsNullOrWhiteSpace(path) ? string.Empty : $"<{path}>; rel=\"{rel}\",";

		private ActionResult<T> PrepareOk<T>(Response<T> response)
		{
			if (response.Notifications.Any())
			{
				var msgs = response.Notifications;
				foreach (var msg in msgs)
				{
					var json = JsonConvert.SerializeObject(msg);
					Response.Headers.Add("X-Notification", Convert.ToBase64String(Encoding.UTF8.GetBytes(json)));
				}
			}

			return Ok(response.Output);
		}
	}
}
