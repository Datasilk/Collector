import React, { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
//styles
import '@/styles/admin/filter.css';
import './page.css';
//components
import Container from '@/components/admin/container';
import Modal from '@/components/ui/modal';
import Icon from '@/components/ui/icon';
import Select from '@/components/forms/select';
import Input from '@/components/forms/input';
import AdminAddSubject from './components/add';
//context
import { useSession } from '@/context/session';
//api
import { Subjects } from '@/api/user/subjects';
//helpers
import { handleSort, getSortIcon } from '@/helpers/format';
import messages from '@/helpers/messages';

/**
 * <summary>Admin Subjects List Page</summary>
 * <description>Displays and manages the list of subjects in the admin panel.</description>
 */
export default function AdminSubjects() {
    const navigate = useNavigate();
    const session = useSession();
    const { getSubjects, getSubjectsByParent } = Subjects(session);
    
    const [subjects, setSubjects] = useState([]);
    const [showAdd, setShowAdd] = useState(false);
    const [searchName, setSearchName] = useState('');
    const [levelFilters, setLevelFilters] = useState(0);
    const [levelFiltersList, setLevelFiltersList] = useState([
        { id: 0, name: 'All Levels' },
        { id: 1, name: 'Level 1' },
        { id: 2, name: 'Level 2' },
        { id: 3, name: 'Level 3' }
    ]);
    const [sort, setSort] = useState('title ASC'); // Client-side sorting since API doesn't support sorting
    const [deleteModal, setDeleteModal] = useState(null);

    useEffect(() => {
        // Fetch subjects from API
        fetchSubjects();
    }, []);
    
    const fetchSubjects = () => {
        // Use the SubjectListRequestModel properties
        getSubjects('', -1).then(response => {
            if (response.data.success) {
                // Process the subjects to include level information
                const processedSubjects = processSubjectsHierarchy(response.data.data || []);
                setSubjects(processedSubjects);
            } else {
                console.error('Error fetching subjects:', response.data.message);
                messages.error('Failed to fetch subjects');
            }
        }).catch(error => {
            console.error('Error fetching subjects:', error);
            messages.error('Failed to fetch subjects');
        });
    };
    
    // Process subjects to determine their level in hierarchy
    const processSubjectsHierarchy = (subjectsList) => {
        const subjectsMap = {};
        
        // First pass: create a map of all subjects
        subjectsList.forEach(subject => {
            subjectsMap[subject.id] = {
                ...subject,
                level: 1, // Default level
                parentSubject: null,
                children: []
            };
        });
        
        // Second pass: establish parent-child relationships and calculate levels
        subjectsList.forEach(subject => {
            if (subject.parentId > 0 && subjectsMap[subject.parentId]) {
                subjectsMap[subject.id].parentSubject = subjectsMap[subject.parentId].title;
                subjectsMap[subject.id].level = subjectsMap[subject.parentId].level + 1;
                subjectsMap[subject.parentId].children.push(subject.id);
            }
        });
        
        // Convert map back to array
        return Object.values(subjectsMap);
    };

    useEffect(() => {
        if (subjects.length > 0) {
            filterSubjects();
        }
    }, [searchName, levelFilters, sort]);

    const filterSubjects = () => {
        let filtered = [...subjects];
        
        if (searchName) {
            filtered = filtered.filter(subject => 
                subject.title.toLowerCase().includes(searchName.toLowerCase())
            );
        }
        
        if (levelFilters !== 0) {
            filtered = filtered.filter(subject => subject.level === levelFilters);
        }
        
        // Apply sorting
        const [sortField, sortDirection] = sort.split(' ');
        filtered.sort((a, b) => {
            let valueA = a[sortField];
            let valueB = b[sortField];
            
            // Handle special cases
            if (sortField === 'parentSubject') {
                valueA = a.parentSubject || '';
                valueB = b.parentSubject || '';
            } else if (sortField === 'articleCount') {
                valueA = a.articleCount || 0;
                valueB = b.articleCount || 0;
            }
            
            if (typeof valueA === 'string') {
                return sortDirection === 'ASC' ? 
                    valueA.localeCompare(valueB) : 
                    valueB.localeCompare(valueA);
            } else {
                return sortDirection === 'ASC' ? 
                    valueA - valueB : 
                    valueB - valueA;
            }
        });
        
        setSubjects(filtered);
    };

    const handleDelete = (subject) => {
        setDeleteModal(subject);
    };

    const handleDeleteClose = () => {
        setDeleteModal(null);
    };

    const handleDeleteConfirmed = (subjectId) => {
        // Since there's no direct delete endpoint in the API client, we would need to
        // implement this when the API supports it. For now, we'll show an error message.
        messages.error('Subject deletion is not supported by the API at this time');
        handleDeleteClose();
        
        // When API supports deletion, it would look like:
        // deleteSubject(subjectId).then(response => {
        //     if (response.data.success) {
        //         messages.success('Subject deleted successfully');
        //         fetchSubjects(); // Refresh the list
        //     } else {
        //         messages.error(response.data.message || 'Failed to delete subject');
        //     }
        // }).catch(error => {
        //     console.error('Error deleting subject:', error);
        //     messages.error('Failed to delete subject');
        // }).finally(() => {
        //     handleDeleteClose();
        // });
    };

    const DeleteModal = () => {
        return (<>
            <Modal
                title="Delete Subject"
                onClose={handleDeleteClose}
            >
                <p>
                    Do you really want to delete the subject "{deleteModal.title}"?
                    <br />
                    This will remove the subject and potentially affect article categorization.
                </p>
                <div className="buttons">
                    <button className="submit" onClick={() => { handleDeleteConfirmed(deleteModal.id) }}>Yes</button>
                    <button className="cancel" onClick={handleDeleteClose}>Cancel</button>
                </div>
            </Modal>
        </>);
    };

    const handleClosedAddSubject = (subject) => {
        if(subject) {
            // Refresh the subject list from API to get the complete hierarchy
            fetchSubjects();
        }
        setShowAdd(false);
    };

    const tools = (<>
        <button onClick={() => setShowAdd(true)}><Icon name="add"></Icon>New Subject</button>
    </>);

    return (
        <div className="admin-subjects">
            {showAdd && <AdminAddSubject onClose={handleClosedAddSubject} onError={(msg) => messages.error(msg)} />}
            {deleteModal != null && <DeleteModal></DeleteModal>}
            <Container
                title="Subject Management"
                tools={tools}
            >
                <div className="filters">
                    <Input
                        name="subjectsearch"
                        type="text"
                        placeholder="Search by Subject Name"
                        value={searchName}
                        onInput={(e) => setSearchName(e.target.value)}
                        className="nameInput"
                    />
                    <Select
                        options={levelFiltersList.map(level => ({ value: level.id, label: level.name }))}
                        value={levelFilters}
                        onChange={(e) => setLevelFilters(e.target.value)}
                    />
                </div>
                <table className="spreadsheet">
                    <thead>
                        <tr>
                            <th onClick={() => setSort(handleSort('title', sort))}>
                                Subject Name {getSortIcon('title', sort) && <span className="material-symbols-rounded">{getSortIcon('title', sort)}</span>}
                            </th>
                            <th onClick={() => setSort(handleSort('articleCount', sort))}>
                                Articles {getSortIcon('articleCount', sort) && <span className="material-symbols-rounded">{getSortIcon('articleCount', sort)}</span>}
                            </th>
                            <th onClick={() => setSort(handleSort('parentSubject', sort))}>
                                Parent Subject {getSortIcon('parentSubject', sort) && <span className="material-symbols-rounded">{getSortIcon('parentSubject', sort)}</span>}
                            </th>
                            <th onClick={() => setSort(handleSort('level', sort))}>
                                Level {getSortIcon('level', sort) && <span className="material-symbols-rounded">{getSortIcon('level', sort)}</span>}
                            </th>
                            <th></th>
                        </tr>
                    </thead>
                    <tbody>
                        {subjects.map(subject =>
                            <tr 
                                key={subject.id} 
                                onClick={(e) => {
                                    // Prevent triggering if the event originated from action buttons
                                    if (e.target.closest('a')) {
                                        e.stopPropagation();
                                        return;
                                    }
                                    navigate('/admin/subjects/edit/' + subject.id);
                                }}
                            >
                                <td>{subject.title}</td>
                                <td>{subject.articleCount || 0}</td>
                                <td>{subject.parentSubject || 'None'}</td>
                                <td>{subject.level}</td>
                                <td className="buttons">
                                    <Link to={'/admin/subjects/edit/' + subject.id} title="edit subject"><Icon name="edit_square"></Icon></Link>
                                    <Link 
                                        onClick={(e) => { 
                                            e.preventDefault(); 
                                            handleDelete(subject); 
                                        }} 
                                        title="delete subject"
                                    >
                                        <Icon name="delete"></Icon>
                                    </Link>
                                </td>
                            </tr>
                        )}
                    </tbody>
                </table>
            </Container>
        </div>
    );
}
