'use client';

import axios from 'axios';
import moment from 'moment';
import { jwtDecode } from 'jwt-decode';

const UseAxios = ({ user, setUser, useToken = false }) => {

	const api = axios.create({
		baseURL: import.meta.env.VITE_API_URL,
	});

	api.get = (url) => {
		return api({ url, method: 'GET' });
	}

	if (useToken) {
		//use authentication token to gain access to secure remote api
		const getNewToken = async (refreshToken) => {
			const currentUrl = window.location.pathname + window.location.search;
			try {
				const response = await api.post('/auth/refresh-token', { token: refreshToken });
				const newTokenData = response.data.data;

				if (user) {
					user.token = newTokenData.token;
					setUser(user);
				}

				return newTokenData.token;
			} catch (error) {
				console.error('Error refreshing token:', error);
				// Handle error (e.g., logout user if refresh fails)
				localStorage.removeItem('user');
				if(currentUrl.indexOf('/login') < 0) window.location.href = `/login?returl=${encodeURIComponent(currentUrl)}`;
				throw error;
			}
		};

		api.interceptors.request.use(
			async (config) => {
				if(config.url?.indexOf('/auth/refresh-token') >= 0) return config;
				const updatedConfig = { ...config };
				if (user && user.token) {
					const decoded = jwtDecode(user.token);
					if (decoded.exp && decoded.exp - moment().unix() < 10) {
						try {
							const newToken = await getNewToken(user.refreshToken);
							updatedConfig.headers.Authorization = `Bearer ${newToken}`;
						} catch (error) {
							console.log(error);
						}
					} else {
						updatedConfig.headers.Authorization = `Bearer ${user.token}`;
					}
				}

				return updatedConfig;
			},
			(error) => Promise.reject(error)
		);

		api.interceptors.response.use(
			(config) => config,
			async (error) => {
				if (error.response) {
					const currentUrl = window.location.pathname + window.location.search;
					switch (error.response.status) {
						case 401:
						case 403:
						case 404:
							// Dispatch logout action to reset state
							setUser(null);
							console.error('Session expired');
							if(currentUrl.indexOf('/login') < 0) window.location.href = `/login?returl=${encodeURIComponent(currentUrl)}`;
							return true;
						default:
							break;
					}
				}
				return Promise.reject(error);
			}
		);

	}

	return api;
}

export { UseAxios };
