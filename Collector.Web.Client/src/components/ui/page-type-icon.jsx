import Icon from '@/components/ui/icon';

export default function PageTypeIcon({type}){

    const icons = [
        'psychology_alt', //Q&A Sheet
        '',
        '', 
        '',
        '',
        'edit_note', //Slide Editor
    ]
    return (<Icon name={icons[type]}></Icon>);
}