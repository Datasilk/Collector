import React, { useState, useEffect } from 'react';
import '@/styles/admin/filter.css';
import './page.css';
//components
import Container from '@/components/admin/container';
import Modal from '@/components/ui/modal';
import Icon from '@/components/ui/icon';
import Input from '@/components/forms/input';
import Select from '@/components/forms/select';
import TextArea from '@/components/forms/textarea';
import Checkbox from '@/components/forms/checkbox';
//api
import { useSession } from '@/context/session';
import { OpenAI } from '@/api/admin/openai';

const LLM_TYPES = [
    { value: 0, label: 'Local' },
    { value: 1, label: 'Cloud' }
];

export default function AdminOpenAI() {
    const session = useSession();
    const { getAll, add, update, setEnabled, setPreferred, delete: deleteModel } = OpenAI(session);

    const getEmptyForm = () => ({
        modelId: 0,
        name: '',
        model: '',
        endpoint: '',
        privateKey: '',
        type: 1,
        enabled: false,
        preferred: false,
        extraBody: ''
    });

    const [models, setModels] = useState([]);
    const [showModal, setShowModal] = useState(false);
    const [editingModel, setEditingModel] = useState(null);
    const [form, setForm] = useState(getEmptyForm);
    const [error, setError] = useState(null);

    useEffect(() => {
        fetchModels();
    }, []);

    const fetchModels = () => {
        getAll().then(response => {
            if (response.data.success) {
                setModels(response.data.data || []);
            }
        }).catch(error => {
            console.error('Error fetching LLM models:', error);
        });
    };

    const handleAdd = () => {
        setEditingModel(null);
        setForm(getEmptyForm());
        setError(null);
        setShowModal(true);
    };

    const handleEdit = (model) => {
        setEditingModel(model);
        setForm({
            modelId: model.modelId,
            name: model.name || '',
            model: model.model || '',
            endpoint: model.endpoint || '',
            privateKey: model.privateKey || '',
            type: model.type ?? 1,
            enabled: !!model.enabled,
            preferred: !!model.preferred,
            extraBody: model.extraBody || ''
        });
        setError(null);
        setShowModal(true);
    };

    const handleSave = () => {
        if (!form.name || !form.model || !form.endpoint) {
            setError('Name, Model, and Endpoint are required');
            return;
        }

        setError(null);
        const payload = { ...form };
        const action = editingModel ? update : add;
        action(payload).then(response => {
            if (response.data.success) {
                fetchModels();
                setShowModal(false);
            } else {
                setError(response.data.message || 'Failed to save model');
            }
        }).catch(error => {
            console.error('Error saving LLM model:', error);
            setError('Failed to save model');
        });
    };

    const handleToggleEnabled = (model) => {
        const newEnabled = !model.enabled;
        setEnabled(model.modelId, newEnabled).then(response => {
            if (response.data.success) {
                fetchModels();
            } else {
                console.error('Failed to update enabled state:', response.data.message);
            }
        }).catch(error => {
            console.error('Error updating enabled state:', error);
        });
    };

    const handleTogglePreferred = (model) => {
        if (model.preferred) return;
        setPreferred(model.modelId).then(response => {
            if (response.data.success) {
                fetchModels();
            } else {
                console.error('Failed to update preferred state:', response.data.message);
            }
        }).catch(error => {
            console.error('Error updating preferred state:', error);
        });
    };

    const handleDelete = (model) => {
        if (!confirm(`Are you sure you want to delete the model "${model.name}"?`)) return;
        deleteModel(model.modelId).then(response => {
            if (response.data.success) {
                fetchModels();
            }
        }).catch(error => {
            console.error('Error deleting model:', error);
        });
    };

    const handleFormChange = (field, value) => {
        setForm(prev => ({ ...prev, [field]: value }));
    };

    const tools = (
        <button onClick={handleAdd}>
            <Icon name="add"></Icon>Add Model
        </button>
    );

    return (
        <div className="admin-openai">
            {showModal && (
                <Modal title={editingModel ? 'Edit LLM Model' : 'Add LLM Model'} onClose={() => setShowModal(false)}>
                    {error && <div className="error-message">{error}</div>}
                    <div className="form">
                        <Input
                            label="Name"
                            value={form.name}
                            onInput={(e) => handleFormChange('name', e.target.value)}
                            wide={true}
                        />
                        <Input
                            label="Model"
                            value={form.model}
                            onInput={(e) => handleFormChange('model', e.target.value)}
                            wide={true}
                        />
                        <Input
                            label="Endpoint"
                            value={form.endpoint}
                            onInput={(e) => handleFormChange('endpoint', e.target.value)}
                            wide={true}
                        />
                        <Input
                            label="Private Key"
                            value={form.privateKey}
                            type='password'
                            rows={2}
                            onInput={(e) => handleFormChange('privateKey', e.target.value)}
                            wide={true}
                        />
                        <Select
                            label="Type"
                            options={LLM_TYPES}
                            value={form.type}
                            onChange={(e) => handleFormChange('type', parseInt(e.target.value))}
                        />
                        <TextArea
                            label="Extra Body (JSON)"
                            value={form.extraBody}
                            rows={3}
                            onInput={(e) => handleFormChange('extraBody', e.target.value)}
                        />
                        <Checkbox
                            name="enabled"
                            label="Enabled"
                            checked={form.enabled}
                            onChange={(checked) => handleFormChange('enabled', checked)}
                        />
                        <Checkbox
                            name="preferred"
                            label="Preferred"
                            checked={form.preferred}
                            onChange={(checked) => handleFormChange('preferred', checked)}
                        />
                    </div>
                    <div className="buttons">
                        <button className="submit" onClick={handleSave}>Save</button>
                        <button className="cancel" onClick={() => setShowModal(false)}>Cancel</button>
                    </div>
                </Modal>
            )}
            <Container title="Open AI Endpoints" tools={tools}>
                <table className="spreadsheet">
                    <thead>
                        <tr>
                            <th></th>
                            <th>Name</th>
                            <th>Model</th>
                            <th>Endpoint</th>
                            <th>Type</th>
                            <th></th>
                        </tr>
                    </thead>
                    <tbody>
                        {models.map(model => (
                            <tr key={model.modelId} onClick={() => handleEdit(model)}>
                                <td className="checkbox-cell" onClick={(e) => e.stopPropagation()}>
                                    <input
                                        type="checkbox"
                                        checked={model.enabled}
                                        onChange={() => handleToggleEnabled(model)}
                                        title={model.enabled ? 'Disable' : 'Enable'}
                                    />
                                </td>
                                <td>{model.name}</td>
                                <td>{model.model}</td>
                                <td>{model.endpoint}</td>
                                <td>{model.type === 0 ? 'Local' : 'Cloud'}</td>
                                <td className="tool-bar buttons" onClick={(e) => e.stopPropagation()}>
                                    <button
                                        className={"icon " + (model.preferred ? "favorite" : "")}
                                        onClick={() => handleTogglePreferred(model)}
                                        title={model.preferred ? 'Preferred' : 'Set as preferred'}
                                    >
                                        <Icon name={model.preferred ? "star_shine" : "star"} />
                                    </button>
                                    <button
                                        className="icon"
                                        onClick={() => handleEdit(model)}
                                        title="Edit model"
                                    >
                                        <Icon name="edit"></Icon>
                                    </button>
                                    <button
                                        className="icon"
                                        onClick={() => handleDelete(model)}
                                        title="Delete model"
                                    >
                                        <Icon name="delete"></Icon>
                                    </button>
                                </td>
                            </tr>
                        ))}
                        {models.length === 0 && (
                            <tr>
                                <td colSpan="6" style={{ textAlign: 'center', padding: '2rem' }}>
                                    No Open AI endpoints configured. Click "Add Model" to get started.
                                </td>
                            </tr>
                        )}
                    </tbody>
                </table>
            </Container>
        </div>
    );
}
