export default function Icon({ name, ...args }) {
    // https://fonts.google.com/icons
    const options = { 
        ...args,
        className: (args && args.className ? args.className + ' ' : '') + 'material-symbols-rounded' 
    };
    
    return (<span {...options}>{name}</span>);
}