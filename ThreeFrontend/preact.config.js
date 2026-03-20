// module.exports = (config, env, helpers) => {
//     const postCssLoaders = helpers.getLoadersByName(config, 'postcss-loader');
//     postCssLoaders.forEach(({ loader }) => {
//       const plugins = loader.options.postcssOptions.plugins;

//       // Add tailwind css at the top.
//       plugins.unshift(require('tailwindcss'));
//     });
//     return config;
//   };

export default (config) => {
    config.resolve.alias = {
        ...(config.resolve.alias || {}),
        react: 'preact/compat',
        'react-dom': 'preact/compat',
        // 'react-dom/client': 'preact/compat'
        'react-dom/client': require.resolve('./src/react-dom-client-shim')
    };

    return config;
};