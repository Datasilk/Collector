export default function Icon({ name, spin, ...args }) {
    // https://fonts.google.com/icons
    const options = { 
        ...args,
        className: (args && args.className ? args.className + ' ' : '') + 
                  'material-symbols-rounded' + 
                  (spin ? ' icon-spin' : '')
    };
    
    return (<span {...options}>{name}</span>);
}