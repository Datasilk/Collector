const Auth = (api) => {
    const apiPath = '/api/auth';
    return {
        login: (username, password) => api.post(`${apiPath}/login`, {Username: username, Password: password}),
        refreshToken: (token) => api.post(`${apiPath}/refresh-token`, {Token: token}),
        activate: (hash) => api.post(`${apiPath}/activate`, {Hash: hash}),
        forgotPassword: (username) => api.post(`${apiPath}/forgot-password`, {Username: username}),
        checkPasswordReset: (hash) => api.post(`${apiPath}/check-password-reset`, {Hash: hash}),
        updatePassword: (updatePasswordModel) => api.post(`${apiPath}/update-password`, updatePasswordModel),
        oneTimeLogin: (username) => api.post(`${apiPath}/one-time-login`, {Username: username}),
        emailAuth: (token) => api.post(`${apiPath}/email-auth`, {Token: token}),
    };
}

export {Auth};