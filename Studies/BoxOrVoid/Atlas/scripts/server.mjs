import http from 'node:http';
import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../dist');
const port=4177;
const mime={'.html':'text/html; charset=utf-8','.js':'text/javascript; charset=utf-8','.css':'text/css; charset=utf-8','.json':'application/json; charset=utf-8','.svg':'image/svg+xml','.png':'image/png','.webp':'image/webp','.woff2':'font/woff2','.mp3':'audio/mpeg','.mp4':'video/mp4'};
if(!fs.existsSync(path.join(root,'index.html')))throw new Error('Build the atlas first with npm run build.');
const server=http.createServer((req,res)=>{
  let target;
  try{const pathname=decodeURIComponent(new URL(req.url,'http://127.0.0.1').pathname);target=path.resolve(root,'.'+(pathname==='/'?'/index.html':pathname));}catch{res.writeHead(400);res.end();return;}
  if(!target.startsWith(root+path.sep)){res.writeHead(403);res.end();return;}
  fs.stat(target,(error,stat)=>{
    if(error||!stat.isFile()){res.writeHead(404);res.end('Not found');return;}
    const headers={'Content-Type':mime[path.extname(target)]||'application/octet-stream','Accept-Ranges':'bytes','Cache-Control':'no-cache'};
    let start=0,end=stat.size-1,status=200;
    if(req.headers.range){const match=/^bytes=(\d*)-(\d*)$/.exec(req.headers.range);if(!match){res.writeHead(416);res.end();return;}
      if(!match[1])start=Math.max(0,stat.size-Number(match[2]));else{start=Number(match[1]);if(match[2])end=Math.min(end,Number(match[2]));}
      if(start>end||start>=stat.size){res.writeHead(416,{'Content-Range':'bytes */'+stat.size});res.end();return;}
      status=206;headers['Content-Range']=`bytes ${start}-${end}/${stat.size}`;
    }
    headers['Content-Length']=end-start+1;res.writeHead(status,headers);
    if(req.method==='HEAD'){res.end();return;}
    fs.createReadStream(target,{start,end}).on('error',()=>res.destroy()).pipe(res);
  });
});
server.on('error',error=>{console.error(error.message);process.exitCode=1;});
server.listen(port,'127.0.0.1',()=>console.log('Box or Void TD Atlas: http://127.0.0.1:'+port));
