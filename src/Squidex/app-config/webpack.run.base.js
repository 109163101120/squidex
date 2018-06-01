﻿      var webpack = require('webpack'),
     webpackMerge = require('webpack-merge'),
HtmlWebpackPlugin = require('html-webpack-plugin'),
     commonConfig = require('./webpack.config.js'),
          helpers = require('./helpers');

module.exports = webpackMerge(commonConfig, {
    /**
     * The entry point for the bundle
     * Our Angular.js app
     *
     * See: https://webpack.js.org/configuration/entry-context/
     */
    entry: {
        'shims': './app/shims.ts',
          'app': './app/app.ts'
    },

    plugins: [
        /**
         * Simplifies creation of HTML files to serve your webpack bundles.
         * This is especially useful for webpack bundles that include a hash in the filename
         * which changes every compilation.
         *
         * See: https://github.com/ampedandwired/html-webpack-plugin
         */
        new HtmlWebpackPlugin({
            hash: true,
            chunks: ['shims', 'app'],
            chunksSortMode: 'manual',
            template: 'wwwroot/index.html'
        }),
        
        new HtmlWebpackPlugin({
            template: 'wwwroot/theme.html', hash: true, chunksSortMode: 'none', filename: 'theme.html'
        })
    ]
});