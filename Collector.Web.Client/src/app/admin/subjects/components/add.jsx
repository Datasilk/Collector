import React, { useState, useEffect } from 'react';
//components
import Modal from '@/components/ui/modal';
import Icon from '@/components/ui/icon';
import Input from '@/components/forms/input';
import Select from '@/components/forms/select';
//context
import { useSession } from '@/context/session';
//api
import { Subjects } from '@/api/user/subjects';

/**
 * <summary>Add Subject Component</summary>
 * <description>Form and logic for adding a new subject in the admin panel.</description>
 */
export default function AdminAddSubject({ onClose, onError }) {
    const [errors, setErrors] = useState({});
    const [formState, setFormState] = useState('new');
    const [subject, setSubject] = useState({
        title: '',
        parentId: 0,
        grammarType: 0,
        score: 0,
        breadcrumb: ''
    });
    const [parentSubjects, setParentSubjects] = useState([]);
    
    // Grammar types
    const grammarTypes = [
        { id: 0, name: 'None' },
        { id: 1, name: 'Noun' },
        { id: 2, name: 'Verb' },
        { id: 3, name: 'Adjective' }
    ];

    //api
    const session = useSession();
    const { createSubject, getSubjects } = Subjects(session);

    useEffect(() => {
        // Fetch parent subjects
        getSubjects('', -1).then(response => {
            if (response.data.success) {
                setParentSubjects(response.data.data || []);
            }
        }).catch(error => {
            console.error('Error fetching parent subjects:', error);
        });
    }, []);

    const handleChange = (e) => {
        const { name, value } = e.target;
        const data = { ...subject, [name]: value };

        const errs = errors;
        let changed = false;
        if (errs.title && data.title != '') {
            errs.title = null;
            changed = true;
        }
        if (changed == true) {
            setErrors(errs);
        }
        setSubject(data);
    };

    const handleSubmit = (e) => {
        e.preventDefault();
        const newErrors = { exists: false };

        //validate form
        if (!subject.title || subject.title == '') {
            newErrors.title = 'required';
            newErrors.exists = true;
        }
        
        setErrors(newErrors);

        if (!newErrors.exists) {
            //submit form
            setFormState('submitting');
            //add a new subject
            createSubject({
                Title: subject.title,
                ParentId: parseInt(subject.parentId || 0),
                GrammarType: parseInt(subject.grammarType || 0),
                Score: parseInt(subject.score || 0),
                Breadcrumb: subject.breadcrumb || ''
            }).then(response => {
                if (response.data.success) {
                    setFormState('success');
                    onClose(response.data.data);
                } else {
                    setErrors({ ...errors, form: response.data.message });
                    setFormState('error');
                    if (typeof onError == 'function') onError(response.data.message);
                }
            }).catch(() => {
                setErrors({ ...errors, form: 'An error occurred while adding the subject.' });
                setFormState('error');
                if (typeof onError == 'function') onError('An error occurred while adding the subject.');
            });
        }
    };

    return (
        <Modal
            title="Add Subject"
            onClose={onClose}
            error={errors.form}
        >
            <div className="form-sized">
                <form className="admin-form" onSubmit={handleSubmit}>
                    <div className="form-row">
                        <Input
                            label="Subject Title"
                            name="title"
                            value={subject.title || ''}
                            onInput={handleChange}
                            required={true}
                            maxLength={50}
                            placeholder="Enter subject title"
                            error={errors.title}
                        />
                    </div>
                    <div className="form-row">
                        <Select
                            label="Parent Subject"
                            name="parentId"
                            value={subject.parentId || 0}
                            onChange={handleChange}
                            options={[
                                { value: 0, label: 'None (Root Level)' },
                                ...parentSubjects.map(parent => ({ 
                                    value: parent.id, 
                                    label: parent.title 
                                }))
                            ]}
                            error={errors.parentId}
                        />
                    </div>
                    <div className="form-row">
                        <Select
                            label="Grammar Type"
                            name="grammarType"
                            value={subject.grammarType || 0}
                            onChange={handleChange}
                            options={grammarTypes.map(type => ({ 
                                value: type.id, 
                                label: type.name 
                            }))}
                            error={errors.grammarType}
                        />
                    </div>
                    <div className="form-row">
                        <Input
                            label="Score"
                            name="score"
                            type="number"
                            value={subject.score || 0}
                            onInput={handleChange}
                            min="0"
                            placeholder="Subject relevance score"
                            error={errors.score}
                        />
                    </div>
                    <div className="form-row">
                        <Input
                            label="Breadcrumb (Optional)"
                            name="breadcrumb"
                            value={subject.breadcrumb || ''}
                            onInput={handleChange}
                            maxLength={500}
                            placeholder="Enter breadcrumb path (e.g. Science/Physics)"
                            error={errors.breadcrumb}
                        />
                    </div>
                    {formState == 'submitting' ?
                        <div className="submitting">Adding Subject...</div>
                        :
                        <div className="buttons">
                            <button type="submit" className="action-button">
                                <><Icon name="add_circle"></Icon>Add Subject</>
                            </button>
                            <button onClick={onClose} className="cancel"><Icon name="close"></Icon>Cancel</button>
                        </div>
                    }
                </form>
            </div>
        </Modal>
    );
}
