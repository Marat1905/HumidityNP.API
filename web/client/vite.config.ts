import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
    plugins: [react()],
    server: {
        port: 3000,
        proxy: {
            '/humidity/api': {
                target: 'http://localhost:8090',
                changeOrigin: true,
            },
            '/hubs': {
                target: 'http://localhost:8090',
                changeOrigin: true,
                ws: true,
            },
        }
    }
})