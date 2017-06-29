var webpack = require('webpack');

var outPath = __dirname + '/../MirrorBall.Server/wwwroot';
console.log(outPath);

module.exports = {
  entry: './index.tsx',
  output: {
    filename: 'bundle.js',
    path: outPath
  },
  devtool: 'source-map',
  resolve: {
    extensions: ['.webpack.js', '.web.js', '.ts', '.tsx', '.js']
  },
  module: {
    loaders: [
      { test: /\.tsx?$/, loader: 'ts-loader' }
    ]
  },
  plugins: [],
  externals: {}
}
