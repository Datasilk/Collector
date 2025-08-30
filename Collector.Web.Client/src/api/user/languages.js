import { Api } from '@/api/Api';

/**
 * Languages API client
 * @param {Object} args - User session object
 * @returns {Object} - API methods
 */
const Languages = (args) => Api({...args, useToken: true}).endpoints(({api}) => {
    const apiPath = '/api/languages';
    return {
        /**
         * Get all available language codes and their names
         * @returns {Promise} - API response
         */
        getAll: () => api.get(`${apiPath}`),

        /**
         * Get language name by code
         * @param {string} code - ISO 639-1 language code
         * @returns {Promise} - API response
         */
        getByCode: (code) => api.get(`${apiPath}/${code}`),

        /**
         * Validate if a language code is valid
         * @param {string} code - ISO 639-1 language code to validate
         * @returns {Promise} - API response
         */
        validate: (code) => api.get(`${apiPath}/validate/${code}`)
    };
});

export { Languages }
