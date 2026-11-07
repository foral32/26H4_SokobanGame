import {defineConfig} from 'vite';
export default defineConfig({
  build:{rollupOptions:{output:{manualChunks(id){
    if(id.includes('node_modules'))return 'vendor';
    if(id.endsWith('atlas-data.json'))return 'research-data';
    if(id.endsWith('atlas-code.json'))return 'implementation-code';
  }}}}
});
