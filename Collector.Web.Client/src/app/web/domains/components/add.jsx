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
import { Domains } from '@/api/user/domains';

/**
 * <summary>Add Domain Component</summary>
 * <description>Form and logic for adding a new domain in the admin panel.</description>
 */
export default function AdminAddDomain({ onClose, onError }) {
    const [errors, setErrors] = useState({});
    const [formState, setFormState] = useState('new');
    const [domain, setDomain] = useState({
        name: '',
        title: '',
        parentId: 0,
        type: 0,
        status: 'Active',
        category: ''
    });
    
    // Domain types from DomainType enum
    const domainTypes = [
        { id: 0, name: 'Unused' },
        { id: 1, name: 'Website' },
        { id: 2, name: 'Ecommerce' },
        { id: 3, name: 'Wiki' },
        { id: 4, name: 'Blog' },
        { id: 5, name: 'Journal' },
        { id: 6, name: 'SASS' },
        { id: 7, name: 'Social Network' },
        { id: 8, name: 'Advertiser' },
        { id: 9, name: 'Search Engine' },
        { id: 10, name: 'Portfolio' },
        { id: 11, name: 'News' },
        { id: 12, name: 'Travel' },
        { id: 13, name: 'Aggregator' },
        { id: 14, name: 'Government' },
        { id: 15, name: 'Ebooks' },
        { id: 16, name: 'Crypto' },
        { id: 17, name: 'Law' },
        { id: 18, name: 'Medical' },
        { id: 19, name: 'Political' },
        { id: 20, name: 'Software Development' }
        // Note: There are many more domain types in the enum, but showing a subset for UI simplicity
    ];
    const domainRegex = /^(?:[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?\.)+[a-zA-Z]{2,}$/;

    //api
    const session = useSession();
    const { addDomain } = Domains(session);

    const handleChange = (e) => {
        const { name, value } = e.target;
        const data = { ...domain, [name]: value };

        const errs = errors;
        let changed = false;
        if (errs.name && data.name != '') {
            errs.name = null;
            changed = true;
        }
        if (errs.title && data.title != '') {
            errs.title = null;
            changed = true;
        }
        if (changed == true) {
            setErrors(errs);
        }
        setDomain(data);
    };

    const handleSubmit = (e) => {
        e.preventDefault();
        const newErrors = { exists: false };
        
        //validate form
        if (!domain.name || domain.name == '') {
            newErrors.name = 'required';
            newErrors.exists = true;
        } else if (!domainRegex.test(domain.name)) {
            newErrors.name = 'invalid domain format';
            newErrors.exists = true;
        }
        if (!domain.title || domain.title == '') {
            newErrors.title = 'required';
            newErrors.exists = true;
        }
        
        setErrors(newErrors);

        if (!newErrors.exists) {
            //submit form
            setFormState('submitting');
            //add a new domain
            // Using proper casing to match AddDomainModel properties
            addDomain({
                Domain: domain.name,                       // Case-sensitive to match API model
                Title: domain.title || domain.name,        // Use provided title or default to domain name
                ParentId: parseInt(domain.parentId || 0),  // Parent domain ID
                Type: parseInt(domain.type || 0)           // Domain type (0=none, 1=whitelist, 2=blacklist)
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
            title="Add New Domain"
            onClose={onClose}
        >
            <div className="form-sized">
                <form className="admin-form" onSubmit={handleSubmit}>
                    <div className="form-row">
                        <Input
                            label="Domain Name"
                            name="name"
                            value={domain.name || ''}
                            onInput={handleChange}
                            required={true}
                            maxLength={64}
                            placeholder="Enter domain name (e.g. example.com)"
                            error={errors.name}
                        />
                    </div>
                    <div className="form-row">
                        <Input
                            label="Title"
                            name="title"
                            value={domain.title || ''}
                            onInput={handleChange}
                            maxLength={128}
                            placeholder="Enter domain title (optional)"
                            error={errors.title}
                        />
                    </div>
                    <div className="form-row">
                        <Select
                            label="Domain Type"
                            name="type"
                            value={domain.type || 0}
                            onChange={handleChange}
                            options={domainTypes.map(type => ({ value: type.id, label: type.name }))}
                            error={errors.type}
                        />
                    </div>

                    {formState == 'submitting' ?
                        <div className="submitting">Adding Domain...</div>
                        :
                        <div className="buttons">
                            <button type="submit" className="action-button">
                                <><Icon name="add_circle"></Icon>Add Domain</>
                            </button>
                            <button onClick={onClose} className="cancel"><Icon name="close"></Icon>Cancel</button>
                        </div>
                    }
                </form>
            </div>
        </Modal>);
}
