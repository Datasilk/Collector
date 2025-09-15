var svgstore = require('./index')
var gulp = require('gulp')
var cheerio = require('gulp-cheerio')
var replace = require('gulp-replace');


gulp.task('svgs', function () {
  return gulp
    .src('./svgfiles/*.svg')
    .pipe(cheerio({
      run: function ($) {
        $('[fill]').removeAttr('fill')
      },
      parserOptions: { xmlMode: true }
    }))
    .pipe(svgstore())
    .pipe(replace('svg"><defs>', 'svg">\n\n\n' +
      '<style type="text/css">\n' +
      'use:not(.svg-nocolor){fill:currentColor}\n' +
      'use:not(.svg-nocolor):visited{color:currentColor}\n' +
      'use:not(.svg-nocolor):hover{color:currentColor}\n' +
      'use:not(.svg-nocolor):active{color:currentColor}\n' +
      '</style>\n\n\n' +
      '<defs>'))
    .pipe(replace('<svg ', '<svg preserveAspectRatio="none" '))
    .pipe(replace('_0_', '_'))
    .pipe(replace('_FILL', ''))
    .pipe(replace('</symbol>', `</symbol>

`))
    .pipe(replace('</defs>', `</defs>

`))
    .pipe(replace('</style>', `</style>

`))
    .pipe(replace('</g>', `</g>
`))
    .pipe(replace('/>', `/>
`))
    .pipe(replace(`

`, `
`))
    .pipe(replace(`

`, `
`))
    .pipe(replace('<symbol', `
<symbol`))
    .pipe(gulp.dest('../../Collector.Web.Client/public/svgs'));
});