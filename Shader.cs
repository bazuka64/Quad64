using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;

namespace Quad64
{
    public class Shader
    {
        int program;
        Dictionary<string, int> uniformLocations = new Dictionary<string, int>();

        public Shader(string vertPath, string fragPath)
        {
            int vertShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertShader, File.ReadAllText(vertPath));
            CompileShader(vertShader);

            int fragShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragShader, File.ReadAllText(fragPath));
            CompileShader(fragShader);

            program = GL.CreateProgram();
            GL.AttachShader(program, vertShader);
            GL.AttachShader(program, fragShader);
            LinkProgram(program);

            GL.GetProgram(program, GetProgramParameterName.ActiveUniforms, out var numberOfUniforms);
            for(int i = 0; i < numberOfUniforms; i++)
            {
                string name = GL.GetActiveUniform(program, i, out _, out _);
                int location = GL.GetUniformLocation(program, name);
                uniformLocations.Add(name, location);
            }
        }

        void CompileShader(int shader)
        {
            GL.CompileShader(shader);
            GL.GetShader(shader, ShaderParameter.CompileStatus, out var code);
            if (code != (int)All.True)
            {
                var infoLog = GL.GetShaderInfoLog(shader);
                throw new Exception(infoLog);
            }
        }

        void LinkProgram(int program)
        {
            GL.LinkProgram(program);
            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out var code);
            if(code != (int)All.True)
            {
                var infoLog = GL.GetProgramInfoLog(program);
                throw new Exception(infoLog);
            }
        }

        public void Use()
        {
            GL.UseProgram(program);
        }

        public void SetInt(string name, int data)
        {
            GL.Uniform1(uniformLocations[name], data);
        }

        public void SetFloat(string name, float data)
        {
            GL.Uniform1(uniformLocations[name], data);
        }

        public void SetVector3(string name, Vector3 data)
        {
            GL.Uniform3(uniformLocations[name], data);
        }

        public void SetMatrix4(string name, Matrix4 mat)
        {
            GL.UniformMatrix4(uniformLocations[name], false, ref mat);
        }
    }
}