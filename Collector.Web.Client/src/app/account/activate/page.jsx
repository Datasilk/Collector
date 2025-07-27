/**
 * <summary>Account Activation Page</summary>
 * <description>Handles user account activation via emailed link, validates activation hash, and redirects as needed.</description>
 */
import React, { useState, useEffect } from 'react';
import { Link, useParams, useNavigate } from 'react-router-dom';
//components
import Container from '@/components/forms/container';
//api
import { useSession } from '@/context/session';
import { UseAxios } from '@/api/Axios';
import { Auth } from '@/api/account/auth';


export default function ActivateAccount() {
    const { hash } = useParams();
    const navigate = useNavigate();
    const [validHash, setIsValid] = useState(null);

    //api
    const { user, setUser } = useSession();
    const { activate } = Auth(UseAxios({ user, setUser }));

    //execute api
    useEffect(() => {
        activate(hash).then(response => {
            setIsValid(response.data.success);
            if(response.data.data.hasPass === false){
                //redirect to reset password page if user doesn't have a password set yet
                navigate('/create-password/' + hash);
            }
        }).catch(() => {
            setIsValid('err');
        })

    }, [])


    const handleResend = () => {

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
                <Link onClick={handleResend}>Resend Activation Link</Link>
            </Container>
        );

    } else if (validHash === 'err') {
        return (
            <Container
                title="Server Error"
                subtitle=""
                error="We cannot activate your account due to a server error. Please try again later."
            >
            </Container>
        );

    } else if (validHash === true) {
        return (
            <Container
                title="Account Activated!"
                subtitle="You can now log into your account."
            >
            </Container>)
    }

    return (<></>);
}
