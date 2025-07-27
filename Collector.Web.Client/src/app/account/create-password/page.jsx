/**
 * <summary>Create/Reset Password Page</summary>
 * <description>Allows users to set or reset their account password using a secure hash from an email link.</description>
 */
import React, { useState, useEffect } from 'react';
import { Link, useParams } from 'react-router-dom';
//components
import Container from '@/components/forms/container';
import Input from '@/components/forms/input';
//api
import { useSession } from '@/context/session';
import { UseAxios } from '@/api/Axios';
import { Auth } from '@/api/account/auth';

export default function CreatePassword() {
    const { hash } = useParams();
    const [validHash, setIsValid] = useState(null);

    // New state for password fields and feedback
    const [password, setPassword] = useState('');
    const [passwordConfirm, setPasswordConfirm] = useState('');
    const [error, setError] = useState('');
    const [success, setSuccess] = useState('');

    //api
    const { user, setUser } = useSession();
    const { checkPasswordReset, updatePassword } = Auth(UseAxios({ user, setUser }));

    //execute api
    useEffect(() => {
        checkPasswordReset(hash).then(response => {
            setIsValid(response.data.success);
        }).catch(() => {
            setIsValid('err');
        })
    }, []);

    const handleResend = () => {
        // ...unchanged...
    };

    // New handler for updating password
    const handleUpdatePassword = async () => {
        setError('');
        setSuccess('');
        if (password !== passwordConfirm) {
            setError('Passwords do not match.');
            return;
        }
        try {
            // Construct object matching Models\UpdatePassword.cs (PascalCase)
            const updatePasswordModel = {
                Password: password,
                PasswordConfirm: passwordConfirm,
                Hash: hash
            };
            const response = await updatePassword(updatePasswordModel);
            if (response.data && response.data.success) {
                setSuccess('Password updated successfully.');
            } else {
                setError(response.data?.error || 'Failed to update password.');
            }
        } catch {
            setError('Server error. Please try again.');
        }
    };

    if (validHash === null) {
        return (
            <Container
                title="Please wait..."
                subtitle="We are currently checking your account."
            >
            </Container>
        );
    } else if (validHash === false) {
        return (
            <Container
                title="Link is Expired"
                subtitle="The link we gave you have expired."
            >
                <Link onClick={handleResend}></Link>
            </Container>
        );
    } else if (validHash === 'err') {
        return (
            <Container
                title="Server Error"
                subtitle=""
                error="We cannot reset your password due to a server error. Please try again later."
            >
            </Container>
        );
    } else if (success === 'Password updated successfully.') {
        return (
            <Container
                title="Password Updated"
                subtitle=""
                error="Your password has been successfully updated."
            >
                <p>
                    Please <Link to="/login">Log In</Link> to your account to continue.
                </p>
            </Container>
        );
    } else if (validHash === true) {
        return (
            <Container
                title="Create New Password"
                subtitle="Reset your password now."
                error={error}
                success={success}
            >
                <Input
                    type="password"
                    name="password"
                    label="New Password"
                    value={password}
                    onChange={e => setPassword(e.target.value)}
                />
                <Input
                    type="password"
                    name="passwordConfirm"
                    label="Confirm Password"
                    value={passwordConfirm}
                    onChange={e => setPasswordConfirm(e.target.value)}
                />
                <button
                    type="button"
                    onClick={handleUpdatePassword}
                    style={{ marginTop: '1rem' }}
                >
                    Update Password
                </button>
            </Container>
        );
    }

    return (<></>);
}
