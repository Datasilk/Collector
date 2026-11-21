import React, { useState, useEffect } from 'react';
import '@/styles/admin/filter.css';
import './page.css';
//components
import Container from '@/components/admin/container';
import Modal from '@/components/ui/modal';
import Icon from '@/components/ui/icon';
import Input from '@/components/forms/input';
//api
import { useSession } from '@/context/session';
import { Ollama } from '@/api/admin/ollama';
import { OllamaHtmlParsers } from '@/api/admin/ollamaHtmlParsers';

export default function AdminOllama() {
    const session = useSession();
    const { getAll, listAvailable, add, setActive, delete: deleteModel } = Ollama(session);
    const { getAll: getAllParsers, add: addParser, setActive: setActiveParser, delete: deleteParser } = OllamaHtmlParsers(session);

    const [models, setModels] = useState([]);
    const [availableModels, setAvailableModels] = useState([]);
    const [showAdd, setShowAdd] = useState(false);
    const [showDelete, setShowDelete] = useState(null);
    const [newModel, setNewModel] = useState({ id: '', name: '', notes: '' });
    const [isActivating, setIsActivating] = useState(null);
    const [error, setError] = useState(null);

    const [parsers, setParsers] = useState([]);
    const [showAddParser, setShowAddParser] = useState(false);
    const [showDeleteParser, setShowDeleteParser] = useState(null);
    const [newParser, setNewParser] = useState({ id: '', name: '', notes: '' });
    const [isActivatingParser, setIsActivatingParser] = useState(null);
    const [parserError, setParserError] = useState(null);

    useEffect(() => {
        fetchModels();
        fetchAvailableModels();
        fetchParsers();
    }, []);

    const fetchModels = () => {
        getAll().then(response => {
            if (response.data.success) {
                setModels(response.data.data || []);
            }
        }).catch(error => {
            console.error('Error fetching models:', error);
        });
    };

    const fetchAvailableModels = () => {
        listAvailable().then(response => {
            if (response.data.success) {
                setAvailableModels(response.data.data || []);
            } else {
                console.error('Failed to fetch available models:', response.data.message);
            }
        }).catch(error => {
            console.error('Error fetching available models:', error);
        });
    };

    const handleAdd = () => {
        if (!newModel.id || !newModel.name) {
            setError('Please enter both model ID and name');
            return;
        }

        setError(null);
        add(newModel).then(response => {
            if (response.data.success) {
                fetchModels();
                setShowAdd(false);
                setNewModel({ id: '', name: '', notes: '' });
            } else {
                setError(response.data.message || 'Failed to add model');
            }
        }).catch(error => {
            console.error('Error adding model:', error);
            setError('Failed to add model');
        });
    };

    const handleSetActive = (id) => {
        setIsActivating(id);
        setActive(id).then(response => {
            if (!response.data.success) {
                console.error('Failed to activate model:', response.data.message);
            }
        }).catch(error => {
            console.error('Error activating model:', error);
        }).finally(() => {
            setIsActivating(null);
            fetchModels();
        });
    };

    const handleDelete = () => {
        if (!showDelete) return;

        deleteModel(showDelete.id).then(response => {
            if (response.data.success) {
                fetchModels();
                setShowDelete(null);
            }
        }).catch(error => {
            console.error('Error deleting model:', error);
        });
    };

    const fetchParsers = () => {
        getAllParsers().then(response => {
            if (response.data.success) {
                setParsers(response.data.data || []);
            }
        }).catch(error => {
            console.error('Error fetching html parser models:', error);
        });
    };

    const handleAddParser = () => {
        if (!newParser.id || !newParser.name) {
            setParserError('Please enter both model ID and name');
            return;
        }

        setParserError(null);
        addParser(newParser).then(response => {
            if (response.data.success) {
                fetchParsers();
                setShowAddParser(false);
                setNewParser({ id: '', name: '', notes: '' });
            } else {
                setParserError(response.data.message || 'Failed to add model');
            }
        }).catch(error => {
            console.error('Error adding parser model:', error);
            setParserError('Failed to add model');
        });
    };

    const handleSetActiveParser = (id) => {
        setIsActivatingParser(id);
        setActiveParser(id).then(response => {
            if (!response.data.success) {
                console.error('Failed to activate parser model:', response.data.message);
            }
        }).catch(error => {
            console.error('Error activating parser model:', error);
        }).finally(() => {
            setIsActivatingParser(null);
            fetchParsers();
        });
    };

    const handleDeleteParser = () => {
        if (!showDeleteParser) return;

        deleteParser(showDeleteParser.id).then(response => {
            if (response.data.success) {
                fetchParsers();
                setShowDeleteParser(null);
            }
        }).catch(error => {
            console.error('Error deleting parser model:', error);
        });
    };

    const handleCloseAddParser = () => {
        setShowAddParser(false);
        setParserError(null);
    };

    const handleCloseDeleteParser = () => {
        setShowDeleteParser(null);
    };

    const tools = (
        <button onClick={() => setShowAdd(true)}>
            <Icon name="add"></Icon>Add Model
        </button>
    );

    const parserTools = (
        <button onClick={() => setShowAddParser(true)}>
            <Icon name="add"></Icon>Add Model
        </button>
    );

    return (
        <div className="admin-ollama">
            {showAdd && (
                <Modal title="Add Ollama Model" onClose={() => { setShowAdd(false); setError(null); }}>
                    {error && <div className="error-message">{error}</div>}
                    <div className="form">
                        <Input
                            label="Model ID"
                            placeholder="e.g., qwen2.5:0.5b"
                            value={newModel.id}
                            onInput={(e) => setNewModel(prev => ({ ...prev, id: e.target.value }))}
                        />
                        <Input
                            label="Display Name"
                            placeholder="e.g., Qwen 2.5 (0.5B)"
                            value={newModel.name}
                            onInput={(e) => setNewModel(prev => ({ ...prev, name: e.target.value }))}
                        />
                        <Input
                            label="Notes"
                            placeholder="e.g., 0.5B parameters, fast but less accurate"
                            value={newModel.notes}
                            onInput={(e) => setNewModel(prev => ({ ...prev, notes: e.target.value }))}
                        />
                    </div>
                    <div className="buttons">
                        <button className="submit" onClick={handleAdd}>Add Model</button>
                        <button className="cancel" onClick={() => setShowAdd(false)}>Cancel</button>
                    </div>
                </Modal>
            )}
            {showDelete && (
                <Modal title="Delete Model" onClose={() => setShowDelete(null)}>
                    <p>
                        Are you sure you want to delete the model <strong>{showDelete?.name}</strong>?
                        <br />
                        This will only remove it from the database, not from Ollama.
                    </p>
                    <div className="buttons">
                        <button className="submit" onClick={handleDelete}>Delete</button>
                        <button className="cancel" onClick={() => setShowDelete(null)}>Cancel</button>
                    </div>
                </Modal>
            )}
            <Container title="Ollama Models" tools={tools}>
                <table className="spreadsheet">
                    <thead>
                        <tr>
                            <th>Model ID</th>
                            <th>Display Name</th>
                            <th>Notes</th>
                            <th>Status</th>
                            <th></th>
                        </tr>
                    </thead>
                    <tbody>
                        {models.map(model => (
                            <tr key={model.id}>
                                <td>{model.id}</td>
                                <td>{model.name}</td>
                                <td className="notes-cell">{model.notes || '-'}</td>
                                <td>
                                    {model.status === 1 ? (
                                        <span className="badge active">Active</span>
                                    ) : (
                                        <span className="badge inactive">Inactive</span>
                                    )}
                                </td>
                                <td className="tool-bar buttons">
                                    {model.status !== 1 && (
                                        <button
                                            onClick={() => handleSetActive(model.id)}
                                            disabled={isActivating === model.id}
                                            title="Set as active model"
                                        >
                                            {isActivating === model.id ? (
                                                <>Activating...</>
                                            ) : (
                                                <><Icon name="check_circle"></Icon>Set Active</>
                                            )}
                                        </button>
                                    )}
                                    <button
                                        onClick={() => setShowDelete(model)}
                                        title="Delete model"
                                        disabled={model.status === 1}
                                        className="icon"
                                    >
                                        <Icon name="delete"></Icon>
                                    </button>
                                </td>
                            </tr>
                        ))}
                        {models.length === 0 && (
                            <tr>
                                <td colSpan="5" style={{ textAlign: 'center', padding: '2rem' }}>
                                    No models configured. Click "Add Model" to get started.
                                </td>
                            </tr>
                        )}
                    </tbody>
                </table>

            </Container>

            {showAddParser && (
                <Modal title="Add HTML Parser Model" onClose={handleCloseAddParser}>
                    {parserError && <div className="error-message">{parserError}</div>}
                    <div className="form">
                        <Input
                            label="Model ID"
                            placeholder="e.g., milkey/reader-lm-v2"
                            value={newParser.id}
                            onInput={(e) => setNewParser(prev => ({ ...prev, id: e.target.value }))}
                        />
                        <Input
                            label="Display Name"
                            placeholder="e.g., ReaderLM v2"
                            value={newParser.name}
                            onInput={(e) => setNewParser(prev => ({ ...prev, name: e.target.value }))}
                        />
                        <Input
                            label="Notes"
                            placeholder="e.g., HTML to structured JSON parser"
                            value={newParser.notes}
                            onInput={(e) => setNewParser(prev => ({ ...prev, notes: e.target.value }))}
                        />
                    </div>
                    <div className="buttons">
                        <button className="submit" onClick={handleAddParser}>Add Model</button>
                        <button className="cancel" onClick={handleCloseAddParser}>Cancel</button>
                    </div>
                </Modal>
            )}
            {showDeleteParser && (
                <Modal title="Delete Model" onClose={handleCloseDeleteParser}>
                    <p>
                        Are you sure you want to delete the model <strong>{showDeleteParser?.name}</strong>?
                        <br />
                        This will only remove it from the database, not from Ollama.
                    </p>
                    <div className="buttons">
                        <button className="submit" onClick={handleDeleteParser}>Delete</button>
                        <button className="cancel" onClick={handleCloseDeleteParser}>Cancel</button>
                    </div>
                </Modal>
            )}
            <Container title="Ollama: HTML Parser Models" tools={parserTools}>
                <table className="spreadsheet">
                    <thead>
                        <tr>
                            <th>Model ID</th>
                            <th>Display Name</th>
                            <th>Notes</th>
                            <th>Status</th>
                            <th></th>
                        </tr>
                    </thead>
                    <tbody>
                        {parsers.map(model => (
                            <tr key={model.id}>
                                <td>{model.id}</td>
                                <td>{model.name}</td>
                                <td className="notes-cell">{model.notes || '-'}</td>
                                <td>
                                    {model.status === 1 ? (
                                        <span className="badge active">Active</span>
                                    ) : (
                                        <span className="badge inactive">Inactive</span>
                                    )}
                                </td>
                                <td className="tool-bar buttons">
                                    {model.status !== 1 && (
                                        <button
                                            onClick={() => handleSetActiveParser(model.id)}
                                            disabled={isActivatingParser === model.id}
                                            title="Set as active model"
                                        >
                                            {isActivatingParser === model.id ? (
                                                <>Activating...</>
                                            ) : (
                                                <><Icon name="check_circle"></Icon>Set Active</>
                                            )}
                                        </button>
                                    )}
                                    <button
                                        onClick={() => setShowDeleteParser(model)}
                                        title="Delete model"
                                        disabled={model.status === 1}
                                        className="icon"
                                    >
                                        <Icon name="delete"></Icon>
                                    </button>
                                </td>
                            </tr>
                        ))}
                        {parsers.length === 0 && (
                            <tr>
                                <td colSpan="5" style={{ textAlign: 'center', padding: '2rem' }}>
                                    No models configured. Click "Add Model" to get started.
                                </td>
                            </tr>
                        )}
                    </tbody>
                </table>

            </Container>

            <Container title="Available Models in Ollama">
                <table className="spreadsheet">
                    <thead>
                        <tr>
                            <th>Model Name</th>
                            <th>Size</th>
                            <th>Modified</th>
                        </tr>
                    </thead>
                    <tbody>
                        {availableModels.map((model, idx) => (
                            <tr key={idx}>
                                <td>{model.name}</td>
                                <td>{(model.size / 1024 / 1024 / 1024).toFixed(2)} GB</td>
                                <td>{new Date(model.modifiedAt).toLocaleString()}</td>
                            </tr>
                        ))}
                        {availableModels.length === 0 && (
                            <tr>
                                <td colSpan="3" style={{ textAlign: 'center', padding: '2rem' }}>
                                    No models installed in Ollama.
                                </td>
                            </tr>
                        )}
                    </tbody>
                </table>
            </Container>
        </div>
    );
}
