// Intercept fetch requests to include the JWT token
(function () {
    const originalFetch = window.fetch;
    window.fetch = async function (url, options = {}) {
        const token = localStorage.getItem('jwtToken');
        if (token) {
            options.headers = options.headers || {};
            options.headers['Authorization'] = `Bearer ${token}`;
        }
        return originalFetch(url, options);
    };
})();

// Intercept jQuery AJAX requests to include the JWT token
$.ajaxPrefilter(function (options) {
    const token = localStorage.getItem('jwtToken');
    if (token) {
        options.headers = options.headers || {};
        options.headers['Authorization'] = `Bearer ${token}`;
    }
});