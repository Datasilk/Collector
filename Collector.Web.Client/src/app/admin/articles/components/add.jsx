import React, { useState, useEffect } from 'react';
//styles
import '@/styles/forms.css';
//components
import Input from '@/components/forms/input';
import Select from '@/components/forms/select';
import Modal from '@/components/ui/modal';
import Icon from '@/components/ui/icon';
import messages from '@/helpers/messages';
//api
import { useSession } from '@/context/session';
import { Articles } from '@/api/user/articles';
import { Subjects } from '@/api/user/subjects';

/**
 * <summary>Add Article Component</summary>
 * <description>Form and logic for adding a new article in the admin panel.</description>
 */
export default function AdminAddArticle({ onClose, onError }) {
    const [errors, setErrors] = useState({});
    const [formState, setFormState] = useState('new');
    const [article, setArticle] = useState({ isActive: true });
    const [subjects, setSubjects] = useState([]);
    const urlRegex = /^(https?:\/\/)?([\da-z.-]+)\.([a-z.]{2,6})([/\w.-]*)*\/?$/;

    //api
    const session = useSession();
    const { createArticle } = Articles(session);
    const { getSubjects } = Subjects(session);

    useEffect(() => {
        // Fetch subjects for dropdown
        getSubjects().then(response => {
            if (response.data.success) {
                setSubjects(response.data.data || []);
            }
        }).catch(error => {
            console.error('Error fetching subjects:', error);
        });
    }, []);

    const handleChange = (e) => {
        const { name, value } = e.target;
        const data = { ...article, [name]: value };

        const errs = errors;
        let changed = false;
        if (errs.url && data.url != '') {
            errs.url = null;
            changed = true;
        }
        if (errs.title && data.title != '') {
            errs.title = null;
            changed = true;
        }
        if (errs.subject && data.subject != '') {
            errs.subject = null;
            changed = true;
        }
        if (changed == true) {
            setErrors(errs);
        }
        setArticle(data);
    };

    const extractDomain = (url) => {
        if (!url) return '';
        try {
            const urlObj = new URL(url.startsWith('http') ? url : `https://${url}`);
            return urlObj.hostname;
        } catch (e) {
            return '';
        }
    };

    const handleSubmit = (e) => {
        e.preventDefault();
        const newErrors = { exists: false };
        
        //validate form
        if (!article.url || article.url == '') {
            newErrors.url = 'required';
            newErrors.exists = true;
        } else if (!urlRegex.test(article.url)) {
            newErrors.url = 'invalid URL format';
            newErrors.exists = true;
        }
        
        if (!article.title || article.title == '') {
            newErrors.title = 'required';
            newErrors.exists = true;
        }
        
        if (!article.subject || article.subject == '') {
            newErrors.subject = 'required';
            newErrors.exists = true;
        }
        
        setErrors(newErrors);

        if (!newErrors.exists) {
            //submit form
            setFormState('submitting');
            
            // Using proper casing to match ArticleCreateModel properties
            createArticle({
                Url: article.url,
                Title: article.title,
                SubjectId: parseInt(article.subject),
                Domain: extractDomain(article.url),
                FeedId: 0, // Required field in ArticleCreateModel
                Active: article.isActive === true || article.isActive === 'true' // Note: Active not IsActive in the model
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
                setErrors({ ...errors, form: messages.errors.generic });
                setFormState('error');
                if (typeof onError == 'function') onError(messages.errors.generic);
            });
        }
    };

    return (
        <Modal
            title="Add New Article"
            onClose={onClose}
        >
            <div className="form-sized">
                <form className="admin-form" onSubmit={handleSubmit}>
                    <div className="form-row">
                        <Input
                            label="URL"
                            name="url"
                            value={article.url || ''}
                            onInput={handleChange}
                            required={true}
                            maxLength={255}
                            placeholder="Enter article URL"
                            error={errors.url}
                        />
                    </div>
                    <div className="form-row">
                        <Input
                            label="Title"
                            name="title"
                            value={article.title || ''}
                            onInput={handleChange}
                            required={true}
                            maxLength={255}
                            placeholder="Enter article title"
                            error={errors.title}
                        />
                    </div>
                    <div className="form-row">
                        <Select
                            label="Subject"
                            name="subject"
                            value={article.subject || ''}
                            onChange={handleChange}
                            options={subjects.map(subject => ({ value: subject.id, label: subject.title }))}
                            required={true}
                            error={errors.subject}
                        />
                    </div>
                    <div className="form-row checkbox">
                        <label>
                            <input
                                type="checkbox"
                                name="isActive"
                                checked={article.isActive === true}
                                onChange={(e) => setArticle({...article, isActive: e.target.checked})}
                            />
                            Active
                        </label>
                    </div>
                    {formState == 'submitting' ?
                        <div className="submitting">Adding Article...</div>
                        :
                        <div className="buttons">
                            <button type="submit" className="action-button">
                                <><Icon name="add_circle"></Icon>Add Article</>
                            </button>
                            <button onClick={onClose} className="cancel"><Icon name="close"></Icon>Cancel</button>
                        </div>
                    }
                </form>
            </div>
        </Modal>);
}
