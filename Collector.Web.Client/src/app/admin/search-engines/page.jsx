import React, { useState, useEffect, useRef } from 'react';
import '@/styles/admin/filter.css';
import './page.css';
//components
import Container from '@/components/admin/container';
import Modal from '@/components/ui/modal';
import Icon from '@/components/ui/icon';
import Input from '@/components/forms/input';
import Checkbox from '@/components/forms/checkbox';
//api
import { useSession } from '@/context/session';
import { SearchEngines } from '@/api/admin/search-engines';

export default function AdminSearchEngines() {
    const session = useSession();
    const { getAll, add, update, setEnabled, reorder, delete: deleteEngine } = SearchEngines(session);

    const getEmptyForm = () => ({
        searchEngineId: 0,
        name: '',
        url: '',
        type: 'web',
        method: 'GET',
        headerVars: '',
        requestBodyVars: '',
        requestBodyDataType: 'form',
        sort: 0,
        enabled: true
    });

    const [engines, setEngines] = useState([]);
    const [showModal, setShowModal] = useState(false);
    const [editingEngine, setEditingEngine] = useState(null);
    const [form, setForm] = useState(getEmptyForm);
    const [error, setError] = useState(null);
    const [dragIndex, setDragIndex] = useState(null);
    const [dragOverIndex, setDragOverIndex] = useState(null);
    const dragItemRef = useRef(null);

    useEffect(() => {
        fetchEngines();
    }, []);

    const fetchEngines = () => {
        getAll().then(response => {
            if (response.data.success) {
                setEngines(response.data.data || []);
            }
        }).catch(error => {
            console.error('Error fetching search engines:', error);
        });
    };

    const handleAdd = () => {
        setEditingEngine(null);
        setForm(getEmptyForm());
        setError(null);
        setShowModal(true);
    };

    const handleEdit = (engine) => {
        setEditingEngine(engine);
        setForm({
            searchEngineId: engine.searchEngineId,
            name: engine.name || '',
            url: engine.url || '',
            type: engine.type || 'web',
            method: engine.method || 'GET',
            headerVars: engine.headerVars || '',
            requestBodyVars: engine.requestBodyVars || '',
            requestBodyDataType: engine.requestBodyDataType || 'form',
            sort: engine.sort ?? 0,
            enabled: !!engine.enabled
        });
        setError(null);
        setShowModal(true);
    };

    const handleSave = () => {
        if (!form.name || !form.url) {
            setError('Name and Url are required');
            return;
        }
        if (form.method === 'GET' && !form.url.includes('{search}')) {
            setError('GET engines must contain the {search} placeholder in the Url');
            return;
        }
        if (form.method === 'POST' && !form.url.includes('{search}') && !form.requestBodyVars.includes('{search}')) {
            setError('POST engines must contain the {search} placeholder in the Url or body vars');
            return;
        }

        setError(null);
        const payload = { ...form };
        const action = editingEngine ? update : add;
        action(payload).then(response => {
            if (response.data.success) {
                fetchEngines();
                setShowModal(false);
            } else {
                setError(response.data.message || 'Failed to save search engine');
            }
        }).catch(error => {
            console.error('Error saving search engine:', error);
            setError('Failed to save search engine');
        });
    };

    const handleToggleEnabled = (engine) => {
        const newEnabled = !engine.enabled;
        setEnabled(engine.searchEngineId, newEnabled).then(response => {
            if (response.data.success) {
                fetchEngines();
            } else {
                console.error('Failed to update enabled state:', response.data.message);
            }
        }).catch(error => {
            console.error('Error updating enabled state:', error);
        });
    };

    const handleDelete = (engine) => {
        if (!confirm(`Are you sure you want to delete the search engine "${engine.name}"?`)) return;
        deleteEngine(engine.searchEngineId).then(response => {
            if (response.data.success) {
                fetchEngines();
            }
        }).catch(error => {
            console.error('Error deleting search engine:', error);
        });
    };

    const handleFormChange = (field, value) => {
        setForm(prev => ({ ...prev, [field]: value }));
    };

    const handleCloseModal = () => {
        setShowModal(false);
    };

    const handleDragStart = (index) => {
        dragItemRef.current = index;
        setDragIndex(index);
    };

    const handleDragOver = (e, index) => {
        e.preventDefault();
        if (dragItemRef.current === null || dragItemRef.current === index) return;

        const from = dragItemRef.current;
        const reordered = [...engines];
        const [moved] = reordered.splice(from, 1);
        reordered.splice(index, 0, moved);
        dragItemRef.current = index;
        setEngines(reordered);
        setDragIndex(index);
    };

    const handleDragEnter = (index) => {
        setDragOverIndex(index);
    };

    const handleDragEnd = () => {
        const ids = engines.map(engine => engine.searchEngineId);
        setDragIndex(null);
        setDragOverIndex(null);
        dragItemRef.current = null;

        reorder(ids).then(response => {
            if (!response.data.success) {
                console.error('Failed to save sort order:', response.data.message);
                fetchEngines();
            }
        }).catch(error => {
            console.error('Error saving sort order:', error);
            fetchEngines();
        });
    };

    const tools = (
        <button onClick={handleAdd}>
            <Icon name="add"></Icon>Add Search Engine
        </button>
    );

    return (
        <div className="admin-search-engines">
            {showModal && (
                <Modal title={editingEngine ? 'Edit Search Engine' : 'Add Search Engine'} onClose={handleCloseModal}>
                    {error && <div className="error-message">{error}</div>}
                    <div className="form">
                        <Input
                            label="Name"
                            value={form.name}
                            onInput={(e) => handleFormChange('name', e.target.value)}
                            wide={true}
                        />
                        <Input
                            label="Url"
                            value={form.url}
                            onInput={(e) => handleFormChange('url', e.target.value)}
                            wide={true}
                        />
                        <p className="field-note">
                            Use {'{search}'} placeholder in the URL query string. (e.g. "https://www.google.com/search?q={'{search}'}")
                        </p>
                        <div className="form-row">
                            <div className="field">
                                <label>Type</label>
                                <select
                                    value={form.type}
                                    onChange={(e) => handleFormChange('type', e.target.value)}
                                >
                                    <option value="web">web</option>
                                    <option value="image">image</option>
                                    <option value="video">video</option>
                                    <option value="news">news</option>
                                    <option value="science-journal">science-journal</option>
                                    <option value="torrent">torrent</option>
                                    <option value="wiki">wiki</option>
                                </select>
                            </div>
                            <div className="field">
                                <label>Method</label>
                                <select
                                    value={form.method}
                                    onChange={(e) => handleFormChange('method', e.target.value)}
                                >
                                    <option value="GET">GET</option>
                                    <option value="POST">POST</option>
                                </select>
                            </div>
                        </div>
                        {form.method === 'POST' && (
                            <>
                                <Input
                                    label="Request Body Vars (JSON)"
                                    value={form.requestBodyVars}
                                    onInput={(e) => handleFormChange('requestBodyVars', e.target.value)}
                                    wide={true}
                                />
                                <p className="field-note">
                                    JSON object of POST fields, e.g. {'{"q": "{search}", "category": "all"}'}
                                </p>
                                <div className="form-row">
                                    <div className="field">
                                        <label>Body Data Type</label>
                                        <select
                                            value={form.requestBodyDataType}
                                            onChange={(e) => handleFormChange('requestBodyDataType', e.target.value)}
                                        >
                                            <option value="form">form</option>
                                            <option value="json">json</option>
                                        </select>
                                    </div>
                                </div>
                            </>
                        )}
                        <Input
                            label="Header Vars (JSON, optional)"
                            value={form.headerVars}
                            onInput={(e) => handleFormChange('headerVars', e.target.value)}
                            wide={true}
                        />
                        <p className="field-note">
                            JSON object of request headers, e.g. {'{"Accept": "application/json"}'}
                        </p>
                        <Checkbox
                            name="enabled"
                            label="Enabled"
                            checked={form.enabled}
                            onChange={(checked) => handleFormChange('enabled', checked)}
                        />
                    </div>
                    <div className="buttons">
                        <button className="submit" onClick={handleSave}>Save</button>
                        <button className="cancel" onClick={handleCloseModal}>Cancel</button>
                    </div>
                </Modal>
            )}
            <Container title="Search Engines" tools={tools}>
                <table className="spreadsheet">
                    <thead>
                        <tr>
                            <th></th>
                            <th></th>
                            <th>Name</th>
                            <th>Type</th>
                            <th>Method</th>
                            <th>Url</th>
                            <th></th>
                        </tr>
                    </thead>
                    <tbody>
                        {engines.map((engine, index) => (
                            <tr
                                key={engine.searchEngineId}
                                className={dragOverIndex === index && dragIndex !== index ? 'drag-over' : ''}
                                onDragOver={(e) => handleDragOver(e, index)}
                                onDragEnter={() => handleDragEnter(index)}
                                onDragEnd={handleDragEnd}
                                onClick={() => handleEdit(engine)}
                            >
                                <td
                                    className="drag-handle"
                                    draggable="true"
                                    onDragStart={() => handleDragStart(index)}
                                    onClick={(e) => e.stopPropagation()}
                                    title="Drag to change priority"
                                >
                                    <Icon name="drag_indicator" />
                                </td>
                                <td className="checkbox-cell" onClick={(e) => e.stopPropagation()}>
                                    <input
                                        type="checkbox"
                                        checked={engine.enabled}
                                        onChange={() => handleToggleEnabled(engine)}
                                        title={engine.enabled ? 'Disable' : 'Enable'}
                                    />
                                </td>
                                <td>{engine.name}</td>
                                <td>{engine.type || 'web'}</td>
                                <td>{engine.method || 'GET'}</td>
                                <td>{engine.url}</td>
                                <td className="tool-bar buttons" onClick={(e) => e.stopPropagation()}>
                                    <button
                                        className="icon"
                                        onClick={() => handleEdit(engine)}
                                        title="Edit search engine"
                                    >
                                        <Icon name="edit"></Icon>
                                    </button>
                                    <button
                                        className="icon"
                                        onClick={() => handleDelete(engine)}
                                        title="Delete search engine"
                                    >
                                        <Icon name="delete"></Icon>
                                    </button>
                                </td>
                            </tr>
                        ))}
                        {engines.length === 0 && (
                            <tr>
                                <td colSpan="7" style={{ textAlign: 'center', padding: '2rem' }}>
                                    No search engines configured. Click "Add Search Engine" to get started.
                                </td>
                            </tr>
                        )}
                    </tbody>
                </table>
            </Container>
        </div>
    );
}
