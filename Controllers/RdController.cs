using System;
using DocsRd.Models;
using Nskd;
using System.Collections;
using System.Web.Mvc;

namespace DocsRd.Controllers
{
    public class RdController : Controller
    {
        public Object Index(Guid sessionId)
        {
            Object result = $"DocsRd.Controllers.RdController.Index({sessionId})<br />";
            try
            {
                TempData["html"] = FileTree.RenderDirectoryTree(sessionId, null);
                result = PartialView(sessionId);
            }
            catch (Exception e) { result += e.ToString(); }
            return result;
        }
        public Object DownloadFile(String path) 
        {
            path = Utility.UnEscape(path);
            Object result = $"DocsRd.Controllers.RdController.DownloadFile('{path}')<br />";
            try
            {
                FileData fd = FileData.GetFile(path);
                result = File(fd.Contents, fd.ContentType, fd.Name);
            }
            catch (Exception e) { result += e.ToString(); }
            return result;
        }
        public Object GetDirectoryInfo(String path)
        {
            path = Utility.UnEscape(path);
            Object result = $"DocsRd.Controllers.RdController.GetDirectoryInfo('{path}')<br />";
            Guid sessionId = new Guid();
            result = FileTree.RenderDirectoryTree(sessionId, path);
            return result;
        }
        public Object GetFileInfo(String path)
        {
            path = Utility.UnEscape(path);
            Object result = $"Controllers.RdController.GetFileInfo('{path}')<br />";
            RdInf rdInf = new RdInf();
            rdInf.GetFileInfo(path);
            result = PartialView("~/Views/Rd/Inf.cshtml", rdInf);
            return result;
        }
        public Object SetFileInfo(String data)
        {
            Object result = $"Controllers.RdController.Test('{data}')<br />";
            Hashtable ht = Nskd.JsonV3.Parse(data) as Hashtable;
            if (ht != null)
            {
                result += ht.Count.ToString();
            }
            RdInf rdInf = new RdInf(ht);
            rdInf.SetFileInfo();
            return result;
        }
    }
}
