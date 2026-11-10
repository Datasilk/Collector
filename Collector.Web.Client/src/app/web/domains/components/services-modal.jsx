import React, { useState, useEffect } from 'react';
import Modal from '@/components/ui/modal';
import Input from '@/components/forms/input';
import Checkbox from '@/components/forms/checkbox';
import Icon from '@/components/ui/icon';
import Pagination from '@/components/ui/pagination';
import { Domains } from '@/api/user/domains';

const ServicesModal = ({ onClose, onSave, session, selectedServices = [] }) => {
    const [search, setSearch] = useState('');
    const [services, setServices] = useState(null);
    const [loading, setLoading] = useState(false);
    const [selectedIds, setSelectedIds] = useState([]);
    const [searchTimer, setSearchTimer] = useState(null);
    const [pagination, setPagination] = useState({
        currentPage: 1,
        pageSize: 50,
        totalItems: 0,
        totalPages: 1
    });
    
    const { getDomainServices } = Domains(session);

    useEffect(() => {
        if (selectedServices?.length > 0) {
            setSelectedIds(selectedServices.map(s => s.id));
        }
    }, []);

    const loadServices = async (searchQuery = '', page = 1) => {
        setLoading(true);
        try {
            const start = (page - 1) * pagination.pageSize;
            const response = await getDomainServices(searchQuery, start, pagination.pageSize);
            if (response.data && response.data.success) {
                setServices(response.data.data.services || []);
                const totalItems = response.data.data.totalCount || 0;
                setPagination(prev => ({
                    ...prev,
                    currentPage: page,
                    totalItems,
                    totalPages: Math.ceil(totalItems / pagination.pageSize) || 1
                }));
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
            setPagination(prev => ({...prev, currentPage: 1})); // Reset to first page when searching
            loadServices(value);
        }, 1500);
        
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
        <Modal isOpen={true} onClose={onClose} title="Domain Services">
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
                    ) : services != null && services.length === 0 ? (
                        <div className="empty short no-results">No services found</div>
                    ) : services == null ? (<div className="empty short begin">You can search for domains based on the various services they offer on their websites.</div>) 
                    : (<>
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
                        
                        <div className="pagination-container">
                            <Pagination
                                currentPage={pagination.currentPage}
                                totalPages={pagination.totalPages}
                                onPageChange={(page) => loadServices(search, page)}
                                showPageNumbers={true}
                                showPageSizeSelector={false}
                            />
                        </div>
                        
                        <div className="buttons">
                            <button onClick={onClose} className="cancel">Cancel</button>
                            <button onClick={handleSave}>Apply</button>
                        </div>
                    </>)}
                </div>
            </div>
        </Modal>
    );
};

export default ServicesModal;
