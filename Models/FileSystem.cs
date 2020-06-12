using DocsRd.Data;
using System;
using System.Collections;
using System.Data;
using System.IO;
using System.Net;
using System.Text;

namespace DocsRd.Models
{
    public class FileData
    {
        public String Name { get { return Path.GetFileName(FullName); } private set { } }
        public String FullName { get; set; }
        public String ContentType { get; set; }
        public Byte[] Contents { get; set; }
        public static FileData GetFile(String path)
        {
            Guid sessionId = new Guid();
            FileData fd = new FileData();
            fd.FullName = path;
            fd.Contents = Fs.GetFileContents(sessionId, path);
            fd.ContentType = "text/plain";
            String[] parts = path.Split('.');
            String ext = parts[parts.Length - 1].ToLower();
            switch (ext)
            {
                case "pdf":
                    fd.ContentType = "multipart/encrypted";// "application/pdf";
                    break;
                case "xls":
                    fd.ContentType = "application/vnd.ms-excel";
                    break;
                case "xlsx":
                    fd.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    break;
                case "doc":
                    fd.ContentType = "application/msword";
                    break;
                case "docx":
                    fd.ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                    break;
                /* похоже что это вызывает загрузку не как файла, а как страницы.
                case "htm":
                case "html":
                    fd.ContentType = "text/html";
                    break;
                */
                default:
                    fd.ContentType = "multipart/encrypted";
                    break;
            }

            return fd;
        }
    }
    public class FileTreeNode
    {
        public Guid Id;
        public Guid ParentId;
        public string NodeName;
        public string NodeDescr;
        public String Path;

        public FileTreeNode()
        {
            this.Id = new Guid();
            this.ParentId = new Guid();
            this.NodeName = null;
            this.NodeDescr = null;
            this.Path = "";
        }
        public FileTreeNode(String path, FileTreeNode parent, String nodeName = "dir")
        {
            String[] parts = path.Split('\\');
            String descr = parts[parts.Length - 1];
            this.Id = Guid.NewGuid();
            this.ParentId = (parent == null) ? new Guid() : parent.Id;
            this.NodeName = nodeName;
            this.NodeDescr = descr;
            this.Path = path;
        }

        /*
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("'");
            sb.Append(Id.ToString());
            sb.Append("' : '");
            sb.Append(ParentId.ToString());
            sb.Append("' : '");
            sb.Append(NodeName);
            sb.Append("' : '");
            sb.Append(NodeDescr);
            sb.Append("' : '");
            sb.Append(Path.ToString());
            sb.Append("'");
            return sb.ToString();
        }
        */
    }
    public class FileTree
    {
        private ArrayList Nodes;
        private void LoadDirInfo(DataSet ds, FileTreeNode parentNode = null)
        {
            if (ds != null)
            {
                DataTable dirs = ds.Tables["Directories"];
                // первая строка это родительский каталог
                DataRow pDir = dirs.Rows[0];
                String pDirName = (String)pDir["name"];
                pDirName = ((pDirName == "") ? "\\" : pDirName);
                // добавляем в список
                FileTreeNode pDirNode = new FileTreeNode(pDirName, parentNode, "dir");
                this.Nodes.Add(pDirNode);
                dirs.Rows[0].Delete();
                // остальные строки это вложенные каталоги
                // сортируем в алфавитном порядке
                DataView dv = dirs.DefaultView;
                dv.Sort = "name";
                // добавляем в список
                foreach (DataRowView drv in dv)
                {
                    String cDirName = (String)drv["name"];
                    FileTreeNode cDirNode = new FileTreeNode(cDirName, pDirNode, "dir");
                    this.Nodes.Add(cDirNode);
                }
                // файлы
                DataTable files = ds.Tables["Files"];
                // сортируем в алфавитном порядке
                dv = files.DefaultView;
                dv.Sort = "name";
                // добавляем в список
                foreach (DataRowView drv in dv)
                {
                    String cFileName = (String)drv["name"];
                    FileTreeNode cFileNode = new FileTreeNode(cFileName, pDirNode, "file");
                    this.Nodes.Add(cFileNode);
                }
            }
        }
        private string RenderFileTreeNode(FileTreeNode node, bool isRoot, bool isLast)
        {
            StringBuilder html = new StringBuilder();
            ArrayList children = GetChildNodes(node);
            String line = (isRoot) ? " FsNodeIsRoot" : (isLast) ? " FsNodeIsLast" : " IsMiddleFsNode";
            String expand = (node.NodeName == "file") ? " FsNodeExpandIsLeaf" : (isRoot) ? " FsNodeExpandIsOpen" : " FsNodeExpandIsClosed";
            //String expand = (children.Count == 0) ? " FsNodeExpandIsLeaf" : (isRoot) ? " FsNodeExpandIsOpen" : " FsNodeExpandIsClosed";
            String path = WebUtility.HtmlEncode(node.Path.ToString().Replace('\\', '/'));
            String descr = WebUtility.HtmlEncode(node.NodeDescr);

            html.AppendFormat("<div class='FsNode {0} {1}' data-node-name='{2}' data-path='{3}' >", line, expand, node.NodeName, path);
            {
                html.Append("<div class='FsNodeExpand'></div>");
                html.AppendFormat("<div class='FsNodeContent'>{0}</div>", descr);
                html.Append("<div class='FsNodeContainer'>");
                {
                    for (int i = 0; i < children.Count; i++)
                    {
                        FileTreeNode child = (FileTreeNode)children[i];
                        isLast = (i == (children.Count - 1));
                        html.Append(RenderFileTreeNode(child, false, isLast));
                    }
                }
                html.Append("</div>");
            }
            html.Append("</div>");
            return html.ToString();
        }
        private ArrayList GetChildNodes(FileTreeNode node)
        {
            ArrayList nodes = new ArrayList();
            foreach (object o in this.Nodes)
            {
                FileTreeNode n = (FileTreeNode)o;
                if (n.ParentId != new Guid())
                {
                    if (n.ParentId == node.Id)
                    {
                        nodes.Add(n);
                    }
                }
            }
            return nodes;
        }
        public FileTree()
        {
            this.Nodes = new ArrayList();
        }
        public string ToHtml()
        {
            string html = "";
            // ищем root
            FileTreeNode rootNode = null;
            foreach (object node in this.Nodes)
            {
                if (((FileTreeNode)node).ParentId == new Guid())
                {
                    rootNode = (FileTreeNode)node;
                    break;
                }
            }
            if (rootNode == null) html = "<div>Root node is not found.</div>";
            else html = RenderFileTreeNode(rootNode, true, true);
            return html;
        }
        public static String RenderDirectoryTree(Guid sessionId, String path)
        {
            String html = $"DocsRd.Models.FileTree.RenderDirectoryTree({sessionId}, {path})<br />";
            try
            {
                DataSet ds = Fs.GetDirectoryInfo(sessionId, path);
                FileTree fileTree = new FileTree();
                fileTree.LoadDirInfo(ds);
                html = fileTree.ToHtml();
            }
            catch (Exception e) { html += e.ToString(); }
            return html;
        }

        /*
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (object node in this.Nodes)
            {
                sb.Append(((FileTreeNode)node).ToString());
                sb.Append("<br />");
            }
            return sb.ToString();
        }
        */
    }
    public class RdInf
    {
        public String Path;
        public String Номер;
        public String ДатаРегистрации;
        public String ДатаПеререгистрации;
        public String ДатаОкончания;
        public String Комментарий;
        public RdInf(Hashtable data)
        {
            Path = data.ContainsKey("path") ? data["path"] as String : null;
            if (String.IsNullOrWhiteSpace(Path)) { Path = null; }

            Номер = data.ContainsKey("номер") ? data["номер"] as String : null;
            if (String.IsNullOrWhiteSpace(Номер)) { Номер = null; }

            ДатаРегистрации = data.ContainsKey("дата_регистрации") ? data["дата_регистрации"] as String : null;
            if (String.IsNullOrWhiteSpace(ДатаРегистрации)) { ДатаРегистрации = null; }

            ДатаПеререгистрации = data.ContainsKey("дата_перерегистрации") ? data["дата_перерегистрации"] as String : null;
            if (String.IsNullOrWhiteSpace(ДатаПеререгистрации)) { ДатаПеререгистрации = null; }

            ДатаОкончания = data.ContainsKey("дата_окончания") ? data["дата_окончания"] as String : null;
            if (String.IsNullOrWhiteSpace(ДатаОкончания)) { ДатаОкончания = null; }

            Комментарий = data.ContainsKey("комментарий") ? data["комментарий"] as String : null;
            if (String.IsNullOrWhiteSpace(Комментарий)) { Комментарий = null; }
        }
    }
}
