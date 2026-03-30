import { useState, useEffect } from 'react';
import Icon from '@/components/ui/icon';
import { useSession } from '@/context/session';
import NotesModal from './calendar/notes-modal';
import './calendar.css';

export default function CalendarModule({ module, onUpdate, isEditable = true, tabButtons }) {
    const session = useSession();
    const [currentDate, setCurrentDate] = useState(new Date());
    const [crossedOffDays, setCrossedOffDays] = useState(module.crossedOffDays || []);
    const [dayNotes, setDayNotes] = useState(module.dayNotes || {});
    const [mounted, setMounted] = useState(false);

    useEffect(() => {
        if (mounted) return;
        setMounted(true);

        if (tabButtons) {
            tabButtons([
                {
                    icon: 'arrow_back',
                    title: 'Previous Month',
                    callback: handlePreviousMonth
                },
                {
                    icon: 'arrow_forward',
                    title: 'Next Month',
                    callback: handleNextMonth
                },
                {
                    icon: 'today',
                    title: 'Today',
                    callback: handleToday
                }
            ]);
        }
    }, []);

    const handlePreviousMonth = () => {
        const newDate = new Date(currentDate);
        newDate.setMonth(newDate.getMonth() - 1);
        setCurrentDate(newDate);
    };

    const handleNextMonth = () => {
        const newDate = new Date(currentDate);
        newDate.setMonth(newDate.getMonth() + 1);
        setCurrentDate(newDate);
    };

    const handleToday = () => {
        setCurrentDate(new Date());
    };

    const handleDayClick = (dateString, e) => {
        if (!isEditable) return;
        if (e.target.closest('.day-edit-icon')) return;

        const newCrossedOffDays = crossedOffDays.includes(dateString)
            ? crossedOffDays.filter(d => d !== dateString)
            : [...crossedOffDays, dateString];

        setCrossedOffDays(newCrossedOffDays);
        onUpdate({ ...module, crossedOffDays: newCrossedOffDays, dayNotes });
    };

    const handleEditNote = (dateString, e) => {
        e.stopPropagation();
        
        const handleSaveNote = (date, noteText) => {
            const newDayNotes = { ...dayNotes };
            if (noteText.trim()) {
                newDayNotes[date] = noteText;
            } else {
                delete newDayNotes[date];
            }
            setDayNotes(newDayNotes);
            onUpdate({ ...module, crossedOffDays, dayNotes: newDayNotes });
        };
        
        session.showModal(
            <NotesModal
                dateString={dateString}
                initialNote={dayNotes[dateString] || ''}
                onSave={handleSaveNote}
                onClose={() => session.hideModal()}
            />
        );
    };

    const getDaysInMonth = (date) => {
        const year = date.getFullYear();
        const month = date.getMonth();
        const firstDay = new Date(year, month, 1);
        const lastDay = new Date(year, month + 1, 0);
        const daysInMonth = lastDay.getDate();
        const startingDayOfWeek = firstDay.getDay();

        return { daysInMonth, startingDayOfWeek };
    };

    const formatDateString = (year, month, day) => {
        return `${year}-${String(month + 1).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
    };

    const renderCalendar = () => {
        const { daysInMonth, startingDayOfWeek } = getDaysInMonth(currentDate);
        const year = currentDate.getFullYear();
        const month = currentDate.getMonth();
        const today = new Date();
        const isCurrentMonth = today.getFullYear() === year && today.getMonth() === month;
        const todayDate = today.getDate();

        const days = [];
        const dayNames = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];

        // Add day name headers
        dayNames.forEach(name => {
            days.push(
                <div key={`header-${name}`} className="calendar-day-header">
                    {name}
                </div>
            );
        });

        // Add empty cells for days before the first day of the month
        for (let i = 0; i < startingDayOfWeek; i++) {
            days.push(<div key={`empty-${i}`} className="calendar-day empty"></div>);
        }

        // Add days of the month
        for (let day = 1; day <= daysInMonth; day++) {
            const dateString = formatDateString(year, month, day);
            const isCrossedOff = crossedOffDays.includes(dateString);
            const isToday = isCurrentMonth && day === todayDate;
            const note = dayNotes[dateString];

            days.push(
                <div
                    key={dateString}
                    className={`calendar-day ${isCrossedOff ? 'crossed-off' : ''} ${isToday ? 'today' : ''} ${isEditable ? 'editable' : ''}`}
                    onClick={(e) => handleDayClick(dateString, e)}
                    title={isCrossedOff ? 'Click to unmark' : 'Click to mark as complete'}
                >
                    <span className="day-number">{day}</span>
                    {isEditable && (
                        <div className="tool-bar day-edit-icon">
                            <button 
                                className="icon"
                                onClick={(e) => handleEditNote(dateString, e)}
                                title="Edit note"
                            >
                                <Icon name="edit" />
                            </button>
                        </div>
                    )}
                    {isCrossedOff && <span className="cross-mark">✕</span>}
                    {note && <span className="day-note">{note}</span>}
                </div>
            );
        }

        return days;
    };

    const monthNames = [
        'January', 'February', 'March', 'April', 'May', 'June',
        'July', 'August', 'September', 'October', 'November', 'December'
    ];

    return (
        <div className="calendar-module">
                <div className="calendar-header">
                    <button 
                        className="calendar-nav-btn" 
                        onClick={handlePreviousMonth}
                        title="Previous Month"
                    >
                        <Icon name="chevron_left" />
                    </button>
                    <h3 className="calendar-title">
                        {monthNames[currentDate.getMonth()]} {currentDate.getFullYear()}
                    </h3>
                    <button 
                        className="calendar-nav-btn" 
                        onClick={handleNextMonth}
                        title="Next Month"
                    >
                        <Icon name="chevron_right" />
                    </button>
                </div>
                <div className="calendar-grid">
                    {renderCalendar()}
                </div>
        </div>
    );
}
