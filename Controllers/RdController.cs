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
        public Object Fs(String cmd, String path)
        {
            path = Utility.UnEscape(path);
            Object result = $"DocsRd.Controllers.RdController.GetDirInf('{cmd}', '{path}')<br />";
            String html;
            switch (cmd)
            {
                case "GetFileInfo":
                    html = "GetFileInfo: '" + path + "'";
                    //html = FileTree.GetFileInfo(path);
                    result = html;
                    break;
                case "GetDirectoryInfo":
                    Guid sessionId = new Guid();
                    html = FileTree.RenderDirectoryTree(sessionId, path);
                    result = html;
                    break;
                case "DownloadFile":
                    FileData fd = FileData.GetFile(path);
                    result = File(fd.Contents, fd.ContentType, fd.Name);
                    break;
                default:
                    break;
            }
            return result;
        }
        public Object GetFsInfo(String cmd, String path)
        {
            path = Utility.UnEscape(path);
            Object result = $"Controllers.RdController.GetFsInfo('{cmd}', '{path}')<br />";
            var dt = RdModel.GetFsInfo(path);
            result = PartialView("~/Views/Rd/Inf.cshtml", dt);
            return result;
        }
        public Object Set(String data)
        {
            Object result = $"Controllers.RdController.Test('{data}')<br />";
            
            Hashtable ht = Nskd.JsonV3.Parse(data) as Hashtable;
            if (ht != null)
            {
                result += ht.Count.ToString();
            }
            Models.RdModel.SetFsInfo(ht);
            
            return result;
        }
    }
}
