import React, { useState, useEffect } from 'react';
import Modal from '@/components/ui/modal';
import Input from '@/components/forms/input';
import Checkbox from '@/components/forms/checkbox';
import Icon from '@/components/ui/icon';
import { Domains } from '@/api/user/domains';

const ServicesModal = ({ isOpen, onClose, onSave, session, selectedServices = [] }) => {
    const [search, setSearch] = useState('');
    const [services, setServices] = useState([]);
    const [loading, setLoading] = useState(false);
    const [selectedIds, setSelectedIds] = useState([]);
    const [searchTimer, setSearchTimer] = useState(null);
    
    const { getDomainServices } = Domains(session);

    useEffect(() => {
        if (isOpen) {
            setSelectedIds(selectedServices.map(s => s.id));
            loadServices();
        }
    }, [isOpen]);

    const loadServices = async (searchQuery = '') => {
        setLoading(true);
        try {
            const response = await getDomainServices(searchQuery);
            if (response.data && response.data.success) {
                setServices(response.data.data || []);
            } else {
                console.error('Failed to load services:', response.data?.message);
                setServices([]);
            }
        } catch (error) {
            console.error('Error loading services:', error);
            setServices([]);
        } finally {
            setLoading(false);
        }
    };

    const handleSearchChange = (e) => {
        const value = e.target ? e.target.value : e;
        setSearch(value);
        
        // Clear previous timer
        if (searchTimer) {
            clearTimeout(searchTimer);
            setSearchTimer(null);
        }
        
        // Set new timer
        const timer = setTimeout(() => {
            loadServices(value);
        }, 300);
        
        setSearchTimer(timer);
    };

    const handleCheckboxChange = (id, checked) => {
        if (checked) {
            setSelectedIds(prev => [...prev, id]);
        } else {
            setSelectedIds(prev => prev.filter(serviceId => serviceId !== id));
        }
    };

    const handleSave = () => {
        const selectedServiceObjects = services.filter(service => selectedIds.includes(service.id));
        onSave(selectedServiceObjects);
    };

    return (
        <Modal isOpen={true} onClose={onClose} title="Select Services">
            <div className="services-modal">
                <div className="search-container">
                    <Input 
                        placeholder="Search services..." 
                        value={search} 
                        onChange={handleSearchChange}
                        icon="search"
                    />
                </div>
                
                <div className="services-list">
                    {loading ? (
                        <div className="empty loading">
                            <Icon name="progress_activity" spin={true} /> Loading...
                        </div>
                    ) : services.length === 0 ? (
                        <div className="empty short no-results">No services found</div>
                    ) : (
                        <table className="spreadsheet">
                            <tbody>
                                {services.map(service => (
                                    <tr key={service.id}>
                                        <td>
                                            <Checkbox 
                                                checked={selectedIds.includes(service.id)}
                                                onChange={(checked) => handleCheckboxChange(service.id, checked)}
                                                label={service.name}
                                                name={'service_' + service.id}
                                            />
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    )}
                </div>
                
                <div className="buttons">
                    <button onClick={onClose} className="cancel">Cancel</button>
                    <button onClick={handleSave}>Apply</button>
                </div>
            </div>
        </Modal>
    );
};

export default ServicesModal;
