'use strict';

//includes
var gulp = require('gulp'),
    less = require('gulp-less'),
    replace = require('gulp-replace'),
    concat = require('gulp-concat'),
    fs = require('fs'),
    headerfooter = require('gulp-headerfooter');

//paths
var paths = {
    css: 'CSS/',
    scripts: 'Scripts/',
    webroot: 'wwwroot/',
};

//working paths
paths.working = {
    js: {
        app: paths.scripts + 'app.js',
        app_files: [
            paths.scripts + 'app/*.js',
            paths.scripts + 'components/ui/*.js',
            paths.scripts + 'utils/*.js',
            paths.scripts + 'init.js',
        ],
        login_files: [
            paths.scripts + 'app/dark-mode.js',
            paths.scripts + 'app/ajax.js',
            paths.scripts + 'app/toggle.js',
            paths.scripts + 'components/ui/darkmode-toggle.js',
            paths.scripts + 'init.js',
            paths.scripts + 'login.js',
        ]
    },
    less: {
        app: paths.css + 'app.less',
        app_files: [
            paths.css + 'app/*.less',
        ],
        login: paths.css + 'login.less',
        login_files: [
            paths.css + 'app/core.less',
            paths.css + 'app/toggle.less',
            paths.css + 'app/dark-mode.less',
        ]
    }
};

//compiled paths
paths.compiled = {
    css: paths.webroot + 'css/',
    js: {
        app: paths.webroot + 'js/app.js',
        login: paths.webroot + 'js/login.js',
    }
};


//tasks for compiling javascript //////////////////////////////////////////////////////////////
const makeAppJs = (files, output) => {
    var app = fs.readFileSync(paths.working.js.app, 'utf8');
    var appParts = app.split('/*[js libraries goes here]*/');
    var p = gulp.src(files, { base: '.' })
        .pipe(concat(output))
        .pipe(headerfooter(appParts[0], appParts[1]));
    return p.pipe(gulp.dest('.', { overwrite: true }));
};

//tasks for compiling LESS & CSS /////////////////////////////////////////////////////////////////////
gulp.task('less:app', function () {
    var p = gulp.src(paths.working.less.app)
        .pipe(less());
    return p.pipe(gulp.dest(paths.compiled.css, { overwrite: true }));
});

gulp.task('less:login', function () {
    var p = gulp.src(paths.working.less.login)
        .pipe(less());
    return p.pipe(gulp.dest(paths.compiled.css, { overwrite: true }));
});

gulp.task('less', gulp.series(['less:app', 'less:login']));

gulp.task('js:app', function () {
    return makeAppJs(paths.working.js.app_files, paths.compiled.js.app);
});

gulp.task('js:login', function () {
    return makeAppJs(paths.working.js.login_files, paths.compiled.js.login);
});

gulp.task('js', gulp.series(['js:app', 'js:login']));

//watch task /////////////////////////////////////////////////////////////////////
gulp.task('watch', function () {
    //watch all specified files for changes
    gulp.watch([paths.working.less.app, paths.working.less.app_files], gulp.series('less:app'));
    gulp.watch([paths.working.less.login, paths.working.less.login_files], gulp.series('less:login'));
    gulp.watch([paths.working.js.app, paths.working.js.app_files], gulp.series('js:app'));
    gulp.watch([paths.working.js.login_files], gulp.series('js:login'));
});

//default task ////////////////////////////////////////////////////////////////////////////
gulp.task('default', gulp.series(['less', 'js', 'watch']));