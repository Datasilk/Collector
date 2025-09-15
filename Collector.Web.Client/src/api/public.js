import axios from 'axios';
import { apiBasePath } from '../helpers/endpoints.js';

const api = axios.create({
	baseURL: apiBasePath(),
});

export default api;
