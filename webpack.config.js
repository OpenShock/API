const {VueLoaderPlugin} = require("vue-loader");
const MiniCssExtractPlugin = require("mini-css-extract-plugin");
const {CleanWebpackPlugin} = require("clean-webpack-plugin");
const path = require("path");
const HtmlWebpackPlugin = require('html-webpack-plugin');
const webpack = require("webpack");

module.exports = {
	entry: {
		main: "./src/main.js",
	},
	output: {
		filename: "[name].[contenthash:8].js",
		path: path.resolve(__dirname, "dist"),
		chunkFilename: "[name].[contenthash:8].js"
	},
	module: {
		rules: [
			{
				test: /\.js$/,
				exclude: /node_modules\/(?!my-module)/,
				use: {
					loader: "babel-loader",
					options: {
						presets: ['@babel/preset-env']
					}
				},
			},
			{
				test: /\.vue$/,
				loader: "vue-loader",
			},
			{
				test: /\.(eot|ttf|woff|woff2)(\?\S*)?$/,
				loader: "file-loader",
				options: {
					name: "[name][contenthash:8].[ext]",
				},
			},
			{
				test: /\.(png|jpe?g|gif|webm|mp4|svg)$/,
				loader: "file-loader",
				options: {
					name: "[name][contenthash:8].[ext]",
					outputPath: "assets/img",
					useRelativePaths: true
				},
			},
			{
				test: /\.s?css$/,
				use: [
					"style-loader",
					{
						loader: MiniCssExtractPlugin.loader,
						options: {
							esModule: false,
						},
					},
					"css-loader",
					"postcss-loader",
					"sass-loader",
				],
			},
		],
	},
	plugins: [
		new VueLoaderPlugin(),
		new CleanWebpackPlugin(),
		new MiniCssExtractPlugin({
			filename: "[name].[contenthash:8].css",
			chunkFilename: "[name].[contenthash:8].css",
		}),
		new HtmlWebpackPlugin({
			template: path.resolve(__dirname, "public", "index.html"),
			favicon: "./public/favicon.ico",
			inject: true
		})
	],
	resolve: {
		extensions: ['', '.js', '.vue'],
		alias: {
			'vue': '@vue/runtime-dom',
			'vuex': 'vuex/dist/vuex.esm-bundler',
			'@': path.join(__dirname, 'src')
		}
	},
	optimization: {
		moduleIds: "deterministic",
		runtimeChunk: "single",
		splitChunks: {
			cacheGroups: {
				vendor: {
					test: /[\\/]node_modules[\\/]/,
					name: "vendors",
					priority: -10,
					chunks: "all",
				},
			},
		},
	},
	devServer: {
		hot: true,
	}
};
