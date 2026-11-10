import { fileURLToPath, URL } from 'node:url';

import { defineConfig, loadEnv } from 'vite';
import fs from 'fs';
import path from 'path';
import child_process from 'child_process';
import { env } from 'process';
//plugins
import react from '@vitejs/plugin-react';
import compress from 'vite-plugin-compression';

const baseFolder =
    env.APPDATA !== undefined && env.APPDATA !== ''
        ? `${env.APPDATA}/ASP.NET/https`
        : `${env.HOME}/.aspnet/https`;

        if (!fs.existsSync(baseFolder)) {
            fs.mkdirSync(baseFolder, { recursive: true });
        }

const certificateName = "collector.web.client";
const certFilePath = path.join(baseFolder, `${certificateName}.pem`);
const keyFilePath = path.join(baseFolder, `${certificateName}.key`);

if (!fs.existsSync(certFilePath) || !fs.existsSync(keyFilePath)) {
    if (0 !== child_process.spawnSync('dotnet', [
        'dev-certs',
        'https',
        '--export-path',
        certFilePath,
        '--format',
        'Pem',
        '--no-password',
    ], { stdio: 'inherit', }).status) {
        throw new Error("Could not create certificate.");
    }
}

// https://vitejs.dev/config/
export default defineConfig(({ mode }) => {
    const viteEnv = loadEnv(mode, process.cwd(), '');
    const useHttp = viteEnv.VITE_USE_HTTP === 'true';
    const port = useHttp ? 7784 : 7783;
    const api_url = env.ASPNETCORE_HTTPS_PORT ? `https://0.0.0.0:${env.ASPNETCORE_HTTPS_PORT}` :
        env.ASPNETCORE_URLS ? env.ASPNETCORE_URLS.split(';')[0] : 'https://0.0.0.0:7252';
    const target = useHttp ? 'http://0.0.0.0:7780' : api_url;

    return {
    plugins: [
        react(),
        compress({
            algorithm: 'gzip',
            ext: '.gz',
            deleteOriginFile: false,
        })],
    resolve: {
        alias: {
            '@': fileURLToPath(new URL('./src', import.meta.url))
        }
    },
    server: {
        host: '0.0.0.0',
        proxy: {
            '^/collector': {
                target,
                secure: false
            }
        },
        port: port,
        ...(useHttp ? {} : {
            https: {
                key: fs.readFileSync(keyFilePath),
                cert: fs.readFileSync(certFilePath),
            }
        })
    },
    build:{
        sourcemap:true,
        assetsInlineLimit:0 //disable embedded inline base64 URLs for assets smaller than 4KiB
    }
    };
})
