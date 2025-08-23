/**
 * <summary>Login Page</summary>
 * <description>Allows users to log in to their account by entering their email and password. Handles authentication and session creation.</description>
 */
import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import './page.css';
//components
import Input from '@/components/forms/input';
//helpers
import messages from '@/helpers/messages';
//api
import { useSession } from '@/context/session';
import { UseAxios } from '@/api/Axios';
import { Auth } from '@/api/account/auth';

export default function Login () {
  const navigate = useNavigate();
  //state
  const [formData, setFormData] = useState({ email: '', password: '' });
  const [errors, setErrors] = useState({});
  const [formState, setFormState] = useState('new');

  //api
  const { user, setUser } = useSession();
  const {login} = Auth(UseAxios({user, setUser}));
  
  const emailRegex = /^[a-zA-Z0-9._+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,10}$/;

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
    if (errs.password && data.password != '') {
      errs.password = null;
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
    if (formData.password == '') {
      newErrors.password = 'required';
      newErrors.exists = true;
    }
    setErrors(newErrors);

    if (!newErrors.exists) {
      //submit form
      setFormState('submitting');
      login(formData.email, formData.password).then(response => {
        if (response.data.success == true) {
          const data = response.data.data;
          setUser(data);
          if(data.roles?.some(a => a == 'admin')){
            navigate('/admin');
          }else{
            navigate('/dashboard');
          }
        } else {
          setErrors({ ...errors, form: response.data.message });
          setFormState('error');
        }
      }).catch((err) => {
        console.log(err);
        setErrors({ ...errors, form: messages.errors.generic });
        setFormState('error');
      });
    }
  };

  const showForm = ['new', 'error'].some(a => a == formState);

  return (
    <div className="login-container">
      <form className="login-form" onSubmit={handleSubmit}>

        {showForm && <>
          <h1>Sign Into Your Account</h1>
          {errors.form && <div className="error-msg">{errors.form}</div>}

          <Input
            label="Email"
            type="email"
            name="email"
            value={formData.email}
            onInput={handleChange}
            required={true}
            maxLength={64}
            placeholder="Enter your email"
            error={errors.email}
          ></Input>
          <Input
            label="Password"
            type="password"
            name="password"
            value={formData.password}
            onInput={handleChange}
            required={true}
            maxLength={16}
            placeholder="Enter your password"
            error={errors.password}
          ></Input>

          <Link to="/forgot-password" className="forgot-password">
            Forgot your password?
          </Link>

          <button type="submit" className="login-button">
            Login
          </button>

        </>}

        {formState == 'submitting' && <>
          <h1>Signing In...</h1>
          <p>Please wait while we verify your account information...</p>
        </>}
      </form>
    </div>
  );
};
