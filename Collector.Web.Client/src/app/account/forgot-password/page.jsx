/**
 * <summary>Forgot Password Page</summary>
 * <description>Allows users to request a password reset link by entering their email address.</description>
 */
import React, { useState } from 'react';
import './page.css';
//components
import Container from '@/components/forms/container';
import Input from '@/components/forms/input';
//api
import { useSession } from '@/context/session';
import { UseAxios } from '@/api/Axios';
import { Auth } from '@/api/account/auth';
//helpers
import messages from '@/helpers/messages';


export default function ForgotPassword() {
    const [formData, setFormData] = useState({
        email: ''
    });
    const [errors, setErrors] = useState({});
    const [formState, setFormState] = useState('new');
    const emailRegex = /^[a-zA-Z0-9._+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,10}$/;

    //api
    const { user, setUser } = useSession();
    const { forgotPassword } = Auth(UseAxios({ user, setUser, useToken: true }));

    const handleChange = (e) => {
        const { name, value } = e.target;
        const data = { ...formData, [name]: value };

        const errs = errors;
        let changed = false;

        if (errs.email && data.email != '') {
            errs.email = null;
            changed = true;
        }
        if (errs.email && emailRegex.test(data.email)) {
            errs.email = null;
            changed = true;
        }

        if (changed == true) {
            setErrors(errs);
        }
        setFormData(data);
    };

    const handleSubmit = (e) => {
        e.preventDefault();
        const newErrors = { exists: false };
        //validate form
        if (formData.email == '') {
            newErrors.email = 'required';
            newErrors.exists = true;
        }
        if (!emailRegex.test(formData.email)) {
            newErrors.email = 'invalid email';
            newErrors.exists = true;
        }
        setErrors(newErrors);

        if (!newErrors.exists) {
            //submit form
            setFormState('submitting');
            forgotPassword(formData.email).then(response => {
                if (response.data.success) {
                    setFormState('success');
                } else {
                    setErrors({ ...errors, form: response.data.message });
                    setFormState('error');
                }
            }).catch(() => {
                setErrors({ ...errors, form: messages.errors.generic });
                setFormState('error');
            });
        }
    };
    
    return (
        <>
            {['new', 'submitting', 'error'].some(a => a == formState) &&
                <Container
                    title="Forgot Password"
                    subtitle="We will do our best to help you recover your account"
                    error={errors.form}
                >
                    <form className="forgot-form" onSubmit={handleSubmit}>

                        <Input
                            label="Email"
                            type="email"
                            name="email"
                            value={formData.email}
                            onChange={handleChange}
                            required={true}
                            placeholder="Enter your email"
                            error={errors.email}
                        ></Input>

                        {formState == 'submitting' ?
                            <div className="submitting">Sending Email...</div>
                            :
                            <button type="submit" className="forgot-button">
                                Submit
                            </button>
                        }
                    </form>
                </Container>
            }
            {formState == 'success' &&
                <Container
                    title="Email Sent!"
                    subtitle="An email was sent to your inbox successfully!"
                >
                    <hr />
                    <p>Please check your inbox for an email that will give you instructions on how to reset your password.</p>
                </Container>
            }
        </>);
};
