import React, { useState } from 'react';
//styles
import '@/styles/forms.css';
//components
import Input from '@/components/forms/input';
import Modal from '@/components/ui/modal';
import Icon from '@/components/ui/icon';
import messages from '@/helpers/messages';
//api
import { useSession } from '@/context/session';
import { Downloads } from '@/api/user/downloads';

/**
 * <summary>Add Download Component</summary>
 * <description>Form and logic for adding a new download in the admin panel.</description>
 */
export default function AdminAddDownload({ onClose, onError }) {
    const [errors, setErrors] = useState({});
    const [formState, setFormState] = useState('new');
    const [download, setDownload] = useState({
        url: '',
        domain: '',
        parentId: 0,
        feedId: 0
    });
    const [domains, setDomains] = useState([]);
    const urlRegex = /^(https?:\/\/)?([\da-z.-]+)\.([a-z.]{2,6})([\w.-]*)*\/?$/;

    //api
    const session = useSession();
    const { addQueueItem } = Downloads(session);

    const handleChange = (e) => {
        const { name, value } = e.target;
        const data = { ...download, [name]: value };

        // If URL is changed, try to extract domain
        if (name === 'url' && value) {
            try {
                const url = new URL(value.startsWith('http') ? value : `https://${value}`);
                data.domain = url.hostname;
            } catch (e) {
                // Invalid URL, can't extract domain
            }
        }

        const errs = errors;
        let changed = false;
        if (errs.url && data.url != '') {
            errs.url = null;
            changed = true;
        }
        if (errs.domain && data.domain != '') {
            errs.domain = null;
            changed = true;
        }
        if (errs.parentId && data.parentId != '') {
            errs.parentId = null;
            changed = true;
        }
        if (errs.feedId && data.feedId != '') {
            errs.feedId = null;
            changed = true;
        }
        if (changed == true) {
            setErrors(errs);
        }
        setDownload(data);
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
        if (!download.url || download.url == '') {
            newErrors.url = 'required';
            newErrors.exists = true;
        } else if (!urlRegex.test(download.url)) {
            newErrors.url = 'invalid URL format';
            newErrors.exists = true;
        }

        // Extract domain from URL if not provided
        const domain = download.domain || extractDomain(download.url);
        if (!domain) {
            newErrors.domain = 'required';
            newErrors.exists = true;
        }

        if (download.parentId < 0) {
            newErrors.parentId = 'Parent ID must be a non-negative integer';
            newErrors.exists = true;
        }

        if (download.feedId < 0) {
            newErrors.feedId = 'Feed ID must be a non-negative integer';
            newErrors.exists = true;
        }

        setErrors(newErrors);

        if (!newErrors.exists) {
            //submit form
            setFormState('submitting');
            //add a new download
            // Using proper casing to match AddQueueItemModel properties
            addQueueItem({
                Url: download.url,     // Case-sensitive to match API model
                Domain: domain,        // Case-sensitive to match API model
                ParentId: download.parentId,           // Default ParentId
                FeedId: download.feedId              // Default FeedId
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
            title="Add New Download"
            onClose={onClose}
        >
            <div className="form-sized">
                <form className="admin-form" onSubmit={handleSubmit}>
                    <div className="form-row">
                        <Input
                            label="URL"
                            name="url"
                            value={download.url || ''}
                            onInput={handleChange}
                            required={true}
                            maxLength={255}
                            placeholder="Enter URL to download"
                            error={errors.url}
                        />
                    </div>
                    <div className="form-row">
                        <Select
                            label="Domain"
                            name="domain"
                            value={download.domain || ''}
                            onChange={handleChange}
                            options={domains.map(domain => ({ value: domain.name, label: domain.name }))}
                            required={true}
                            error={errors.domain}
                        />
                    </div>
                    <div className="form-row">
                        <Input
                            label="Parent ID"
                            name="parentId"
                            type="number"
                            value={download.parentId || 0}
                            onInput={handleChange}
                            min="0"
                            placeholder="Parent domain ID (0 for none)"
                            error={errors.parentId}
                        />
                    </div>
                    <div className="form-row">
                        <Input
                            label="Feed ID"
                            name="feedId"
                            type="number"
                            value={download.feedId || 0}
                            onInput={handleChange}
                            min="0"
                            placeholder="Feed ID (0 for none)"
                            error={errors.feedId}
                        />
                    </div>
                    {formState == 'submitting' ?
                        <div className="submitting">Adding Download...</div>
                        :
                        <div className="buttons">
                            <button type="submit" className="action-button">
                                <><Icon name="add_circle"></Icon>Add Download</>
                            </button>
                            <button onClick={onClose} className="cancel"><Icon name="close"></Icon>Cancel</button>
                        </div>
                    }
                </form>
            </div>
        </Modal>);
}
