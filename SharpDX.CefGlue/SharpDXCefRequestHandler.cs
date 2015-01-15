using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xilium.CefGlue;

namespace SharpDX.Toolkit.CefGlue
{
    public class SharpDXCefRequestHandler : CefRequestHandler
    {
        protected override CefResourceHandler GetResourceHandler(CefBrowser browser, CefFrame frame, CefRequest request)
        {
            return new InternalResourceHandler(); //(browser, frame, request);
        }
    }

    public class InternalResourceHandler : CefResourceHandler
    {
        protected override bool ProcessRequest(CefRequest request, CefCallback callback)
        {
            callback.Continue();
            return true;
        }

        protected override bool CanGetCookie(CefCookie cookie)
        {
            return false;
        }

        protected override bool CanSetCookie(CefCookie cookie)
        {
            return false;
        }

        protected override void Cancel()
        {
            
        }

        string staticContent = @"
<html>
<head>
<style>
html, body{
background:transparent;
color:#FF00FF;
}
</style>
</head>
<body>
HELLO WORLD
</body>
</html>
";

        protected override void GetResponseHeaders(CefResponse response, out long responseLength, out string redirectUrl)
        {
            responseLength = staticContent.Length;
            redirectUrl = "";
        }

        protected override bool ReadResponse(System.IO.Stream response, int bytesToRead, out int bytesRead, CefCallback callback)
        {
            using (var writer = new System.IO.StreamWriter(response))
            {
                bytesRead = staticContent.Length;
                writer.Write(staticContent);
            }
            return true;
        }
    }
}
